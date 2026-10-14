using System.Text.Json.Serialization;

namespace Practica2.StructuredOutput;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Prioridad
{
    Baja,
    Media,
    Alta
}

// El modelo extrae ENTIDADES (texto). C# resuelve la fecha real.
// JSON esperado: { "titulo": "...", "diaSemana": "miercoles"|null, "hora": "18:00"|null, "prioridad": "Media" }
public sealed record Tarea(
    [property: JsonPropertyName("titulo")] string Titulo,
    [property: JsonPropertyName("diaSemana")] string? DiaSemana,
    [property: JsonPropertyName("hora")] string? Hora,
    [property: JsonPropertyName("prioridad")] Prioridad Prioridad = Prioridad.Media)
{
    private static readonly Dictionary<string, DayOfWeek> Dias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["domingo"] = DayOfWeek.Sunday,
        ["lunes"] = DayOfWeek.Monday,
        ["martes"] = DayOfWeek.Tuesday,
        ["miercoles"] = DayOfWeek.Wednesday,
        ["miércoles"] = DayOfWeek.Wednesday,
        ["jueves"] = DayOfWeek.Thursday,
        ["viernes"] = DayOfWeek.Friday,
        ["sabado"] = DayOfWeek.Saturday,
        ["sábado"] = DayOfWeek.Saturday,
    };

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Titulo))
            throw new InvalidOperationException("El JSON vino sin 'titulo'.");
        if (DiaSemana is not null && !Dias.ContainsKey(DiaSemana.Trim()))
            throw new InvalidOperationException($"'diaSemana' inválido: '{DiaSemana}'.");
        if (Hora is not null && !TimeOnly.TryParse(Hora.Trim(), out _))
            throw new InvalidOperationException($"'hora' inválida: '{Hora}' (esperado HH:mm).");
    }

    // C# calcula la fecha: determinístico, el modelo nunca hace aritmética de fechas.
    public DateTime? ResolverFecha(DateTime hoy)
    {
        if (DiaSemana is null && Hora is null)
            return null;

        var fecha = hoy.Date;
        if (DiaSemana is not null)
        {
            var objetivo = Dias[DiaSemana.Trim()];
            var delta = ((int)objetivo - (int)fecha.DayOfWeek + 7) % 7;
            fecha = fecha.AddDays(delta);
        }
        if (Hora is not null && TimeOnly.TryParse(Hora.Trim(), out var t))
            return fecha.Add(t.ToTimeSpan());

        return fecha;
    }
}
