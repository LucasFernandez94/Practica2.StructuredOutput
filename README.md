# Práctica 2 — Structured Output (.NET + Ollama local)

Convierte pedidos en lenguaje natural en un objeto C# validado, usando `format: "json"` de Ollama. Sin NuGets: `HttpClient` puro contra `http://localhost:11434/api/chat`.

```bash
dotnet run
```

## El patrón (la lección principal)

El modelo **extrae entidades**, C# **calcula y valida**:

texto libre → `{titulo, diaSemana, hora, prioridad}` → `Tarea.ResolverFecha()` → `DateTime` real

El modelo nunca hace aritmética de fechas (los modelos chicos la pifian: meses inventados, días corridos). Misma regla que en la P3: no confíes en tipos ni cálculos del modelo.

## Archivos

- `Tarea.cs` — record + enum `Prioridad` + `Validar()` + `ResolverFecha(hoy)` (próximo día de semana en C#, determinístico)
- `StructuredClient.cs` — POST con `format:"json"`, limpia fences ` ```json `, deserializa case-insensitive, **reintenta 1 vez** ante JSON inválido o que no cumple el schema
- `Program.cs` — 2 demos + loop interactivo (`/salir`, `/modelo <nombre>`)

## Modelo recomendado (CPU sin GPU)

`qwen2.5:1.5b` (rápido, ~2-4s). `llama3.2:3b` razona fechas algo mejor pero más lento. `granite3.3:2b` es rápido pero flojo en fechas.

## Ejemplo

```
Pedido > agendar turno para daniel el miercoles a las 18 hs
  JSON crudo : {"titulo": "Turno de Daniel", "diaSemana": "miércoles", "hora": "18:00"}
  Entidades  : titulo="Turno de Daniel", dia=miércoles, hora=18:00, prio=Media
  Fecha C#   : 2026-10-14 18:00 (Wednesday) [1.6s]
```
