using System.Diagnostics;
using Practica2.StructuredOutput;

using var client = new StructuredClient(model: "qwen2.5:1.5b");

Console.WriteLine($"=== Práctica 2 · Structured Output ({client.Model}) ===");
Console.WriteLine("El modelo extrae entidades (día + hora en texto) y C# calcula la fecha real.");
Console.WriteLine("Comandos: /salir · /modelo <nombre>\n");

// Demo automática: un caso con día/hora y uno sin fecha
foreach (var demo in new[] { "Agendame dentista mañana a las 10", "Comprar pan, urgente" })
{
    await ProcesarAsync(demo);
}

while (true)
{
    Console.Write("Pedido > ");
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("/salir", StringComparison.OrdinalIgnoreCase)) break;
    if (input.StartsWith("/modelo ", StringComparison.OrdinalIgnoreCase))
    {
        client.Model = input["/modelo ".Length..].Trim();
        Console.WriteLine($"(modelo -> {client.Model})\n");
        continue;
    }

    await ProcesarAsync(input);
}

Console.WriteLine("Chau!");

async Task ProcesarAsync(string texto)
{
    try
    {
        var sw = Stopwatch.StartNew();
        var (tarea, crudo) = await client.ExtraerTareaAsync(texto);
        sw.Stop();
        var fecha = tarea.ResolverFecha(DateTime.Now);
        Console.WriteLine($"  JSON crudo : {crudo}");
        Console.WriteLine($"  Entidades  : titulo=\"{tarea.Titulo}\", dia={tarea.DiaSemana ?? "null"}, hora={tarea.Hora ?? "null"}, prio={tarea.Prioridad}");
        Console.WriteLine($"  Fecha C#   : {(fecha?.ToString("yyyy-MM-dd HH:mm (dddd)") ?? "sin fecha")} [{sw.Elapsed.TotalSeconds:F1}s]\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [error] {ex.Message}\n");
    }
}
