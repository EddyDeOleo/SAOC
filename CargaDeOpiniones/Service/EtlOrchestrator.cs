
using CargaDeEncuestasInternas.Entities.API;
using CargaDeEncuestasInternas.Entities.Csv;
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.dboSchema;
using CargaDeEncuestasInternas.Models.DTOs;
using System.Diagnostics;

namespace CargaDeEncuestasInternas.Service;

public class EtlOrchestrator
{
    private readonly IExtractor<Encuesta> _csvExtractor;
    private readonly IExtractor<Resena> _dbExtractor;
    private readonly IExtractor<ComentarioSocialDto> _apiExtractor;
    private readonly IStagingRepository _staging;
    private readonly ILoggerService _logger;

    public EtlOrchestrator(
        IExtractor<Encuesta> csvExtractor,
        IExtractor<Resena> dbExtractor,
        IExtractor<ComentarioSocialDto> apiExtractor,
        IStagingRepository staging,
        ILoggerService logger)
    {
        _csvExtractor = csvExtractor;
        _dbExtractor = dbExtractor;
        _apiExtractor = apiExtractor;
        _staging = staging;
        _logger = logger;
    }

    public async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        // ── Stopwatch GLOBAL — mide el ciclo completo ─────────
        var swGlobal = Stopwatch.StartNew();
        int totalRegistros = 0;
        int totalErrores = 0;

        _logger.LogInfo("╔══════════════════════════════════════════════════╗");
        _logger.LogInfo("║       EtlOrchestrator: INICIO DE EXTRACCIÓN      ║");
        _logger.LogInfo("╚══════════════════════════════════════════════════╝");

        // ── CSV → Encuestas ───────────────────────────────────
        var (regCsv, errCsv) = await ExtraerYPersistirAsync(
            _csvExtractor,
            datos => datos.Select(e => new OpinionExtraidaDto
            {
                CodigoOriginal = e.IdOpinion.ToString(),
                IdCliente = e.IdCliente.ToString(),
                IdProducto = e.IdProducto.ToString(),
                Fecha = e.Fecha.ToString("yyyy-MM-dd"),
                Comentario = e.Comentario?.Trim() ?? string.Empty,
                Clasificacion = e.Clasificacion?.Trim(),
                PuntajeSatisfaccion = e.PuntajeSatisfaccion,
                FuenteOrigen = "CSV"
            }),
            mapped => _staging.GuardarEncuestasAsync(mapped),
            cancellationToken);

        totalRegistros += regCsv;
        totalErrores += errCsv;

        // ── DATABASE → Reseñas Web ────────────────────────────
        var (regDb, errDb) = await ExtraerYPersistirAsync(
            _dbExtractor,
            datos => datos.Select(r => new OpinionExtraidaDto
            {
                CodigoOriginal = r.idOpinionGlobal.ToString(),
                IdCliente = string.Empty,
                IdProducto = string.Empty,
                Fecha = string.Empty,
                Comentario = string.Empty,
                Rating = r.Rating,
                FuenteOrigen = "DATABASE"
            }),
            mapped => _staging.GuardarResenasAsync(mapped),
            cancellationToken);

        totalRegistros += regDb;
        totalErrores += errDb;

        // ── API → Comentarios Sociales ────────────────────────
        var (regApi, errApi) = await ExtraerYPersistirAsync(
            _apiExtractor,
            datos => datos.Select(c => new OpinionExtraidaDto
            {
                CodigoOriginal = c.IdComentario,
                IdCliente = c.IdCliente,
                IdProducto = c.IdProducto,
                Fecha = c.Fecha,
                Comentario = c.Comentario,
                RedSocial = c.RedSocial,
                FuenteOrigen = "API"
            }),
            mapped => _staging.GuardarComentariosSocialesAsync(mapped),
            cancellationToken);

        totalRegistros += regApi;
        totalErrores += errApi;

        // ── Métricas globales ─────────────────────────────────
        swGlobal.Stop();
        var duracion = swGlobal.Elapsed;
        var cumpleMeta = duracion.TotalMinutes < 5;
        var rendimiento = totalRegistros > 0
            ? (int)(totalRegistros / duracion.TotalSeconds)
            : 0;

        _logger.LogInfo("╔══════════════════════════════════════════════════╗");
        _logger.LogInfo("║         MÉTRICAS DE RENDIMIENTO DEL CICLO        ║");
        _logger.LogInfo("╠══════════════════════════════════════════════════╣");
        _logger.LogInfo($"║  Total registros procesados : {totalRegistros,10:N0}          ║");
        _logger.LogInfo($"║  Total errores              : {totalErrores,10:N0}          ║");
        _logger.LogInfo($"║  Tiempo total               : {duracion.Minutes:D2}m {duracion.Seconds:D2}s {duracion.Milliseconds:D3}ms      ║");
        _logger.LogInfo($"║  Rendimiento                : {rendimiento,7:N0} registros/seg     ║");
        _logger.LogInfo($"║  Meta < 5 minutos           : {(cumpleMeta ? "✓ CUMPLIDA" : "✗ NO CUMPLIDA"),10}          ║");
        _logger.LogInfo("╚══════════════════════════════════════════════════╝");

        if (!cumpleMeta)
            _logger.LogWarning(
                $"⚠ El ciclo tardó {duracion.TotalMinutes:F2} minutos — excede la meta de 5 min.");
    }

    // ── Helper genérico: extrae → mapea → persiste → log ─────
    // Devuelve (registros procesados, errores)
    private async Task<(int registros, int errores)> ExtraerYPersistirAsync<TResult>(
        IExtractor<TResult> extractor,
        Func<IEnumerable<TResult>, IEnumerable<OpinionExtraidaDto>> mapear,
        Func<IEnumerable<OpinionExtraidaDto>, Task> persistir,
        CancellationToken cancellationToken)
        where TResult : class
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInfo($"── [{extractor.NombreFuente}] Iniciando extracción...");

        try
        {
            var datos = (await extractor.ExtractAsync(cancellationToken)).ToList();
            var mapped = mapear(datos).ToList();

            await persistir(mapped);

            sw.Stop();
            _logger.LogInfo(
                $"── [{extractor.NombreFuente}] ✓ {mapped.Count:N0} registros " +
                $"en {sw.ElapsedMilliseconds:N0}ms " +
                $"({(mapped.Count / (sw.ElapsedMilliseconds / 1000.0)):N0} reg/seg)");

            await _staging.RegistrarLogAsync(
                extractor.NombreFuente, mapped.Count, 0, "COMPLETADO");

            return (mapped.Count, 0);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                $"── [{extractor.NombreFuente}] ✗ ERROR en {sw.ElapsedMilliseconds:N0}ms: {ex.Message}", ex);

            await _staging.RegistrarLogAsync(
                extractor.NombreFuente, 0, 1, "ERROR", ex.Message);

            return (0, 1);
        }
    }
}