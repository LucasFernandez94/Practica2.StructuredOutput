using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace Practica2.StructuredOutput;

public sealed record ChatMessage(string Role, string Content);

// Request a /api/chat con format:"json" para forzar salida JSON
public sealed record ChatRequest(string Model, List<ChatMessage> Messages, bool Stream, string Format);

public sealed class StructuredClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public string Model { get; set; }

    public StructuredClient(string baseUrl = "http://localhost:11434", string model = "llama3.2:3b")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) };
        Model = model;
    }

    // Convierte texto libre en Tarea validada. Devuelve (objeto, jsonCrudo).
    public async Task<(Tarea Tarea, string JsonCrudo)> ExtraerTareaAsync(string texto, CancellationToken ct = default)
    {
        var hoy = DateTime.Now;
        var cultura = new System.Globalization.CultureInfo("es-AR");
        string diaDe(DateTime d) => d.ToString("dddd", cultura).ToLower(cultura);
        var system = $$"""
            Referencia (NO escribas fechas, solo diaSemana): hoy es {{diaDe(hoy)}}, mañana es {{diaDe(hoy.AddDays(1))}}, pasado mañana es {{diaDe(hoy.AddDays(2))}}.
            Extraé del pedido del usuario EXACTAMENTE este JSON, sin texto extra:
            {"titulo": string (breve, obligatorio, nunca vacío), "diaSemana": "lunes"|"martes"|"miercoles"|"jueves"|"viernes"|"sabado"|"domingo"|null (null si no menciona día), "hora": "HH:mm" en 24h o null (null si no menciona hora), "prioridad": "Baja"|"Media"|"Alta" (default "Media")}
            Reglas: "mañana"/"pasado mañana" se resuelven con la referencia de arriba. "urgente"/"urgencia"/"ya" => prioridad "Alta". diaSemana en minúsculas y sin tildes. Respondé SOLO el JSON.
            """;

        var messages = new List<ChatMessage>
        {
            new("system", system),
            new("user", texto)
        };

        var (tarea, crudo) = await PedirYParsearAsync(messages, ct);
        if (tarea is not null) return (tarea, crudo);

        // NOTA: en una app real usarías ILogger/resultado tipado en vez de Console aquí.
        Console.WriteLine("  (primer intento inválido, reintentando con pedido de reparación...)");

        // Reintento de reparación: le mostramos lo que devolvió y le pedimos solo JSON
        messages.Add(new("assistant", crudo));
        messages.Add(new("user", "Eso no fue un JSON válido según el schema. Devolvé SOLO el JSON corregido, sin markdown ni explicaciones."));
        var (tarea2, crudo2) = await PedirYParsearAsync(messages, ct);
        if (tarea2 is null)
            throw new InvalidOperationException($"No pude obtener un JSON válido. Crudo: {crudo2}");

        return (tarea2, crudo2);
    }

    private async Task<(Tarea? Tarea, string Crudo)> PedirYParsearAsync(List<ChatMessage> messages, CancellationToken ct)
    {
        var request = new ChatRequest(Model, messages, Stream: false, Format: "json");
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/chat", request, JsonOptions, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("No pude conectar con Ollama en http://localhost:11434. ¿Está corriendo?", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Ollama devolvió {(int)response.StatusCode}: {body}");
        }

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        var crudo = doc?.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
        crudo = LimpiarFences(crudo);

        try
        {
            var tarea = JsonSerializer.Deserialize<Tarea>(crudo, JsonOptions);
            if (tarea is null) return (null, crudo);
            tarea.Validar();
            return (tarea, crudo);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // JSON malformado O válido pero que no cumple el schema -> se reintenta
            return (null, crudo);
        }
    }

    private static string LimpiarFences(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var inicio = s.IndexOf('\n');
            var fin = s.LastIndexOf("```");
            if (inicio >= 0 && fin > inicio)
                s = s.Substring(inicio + 1, fin - inicio - 1).Trim();
        }
        return s;
    }

    public void Dispose() => _http.Dispose();
}
