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

    private readonly IEnumerable<IDimensionLoader<OpinionExtraidaDto>> _dimLoaders;
    private readonly IFactLoader<OpinionExtraidaDto> _factLoader;

    private readonly ILoggerService _logger;

    public EtlOrchestrator(
        IExtractor<Encuesta> csvExtractor,
        IExtractor<Resena> dbExtractor,
        IExtractor<ComentarioSocialDto> apiExtractor,
        IStagingRepository staging,
        ILoggerService logger,
        IEnumerable<IDimensionLoader<OpinionExtraidaDto>> dimLoaders, 
        IFactLoader<OpinionExtraidaDto> factLoader)
    {
        _csvExtractor = csvExtractor;
        _dbExtractor = dbExtractor;
        _apiExtractor = apiExtractor;
        _staging = staging;
        _logger = logger;
        _dimLoaders = dimLoaders;
        _factLoader = factLoader;
    }

    public async Task EjecutarEtlCompletoAsync(CancellationToken cancellationToken)
    {
        var swGlobal = Stopwatch.StartNew();

        _logger.LogInfo("╔══════════════════════════════════════════════════╗");
        _logger.LogInfo("║        INICIO DE PROCESO ETL (E + L)             ║");
        _logger.LogInfo("╚══════════════════════════════════════════════════╝");

        await FaseExtraccionAsync(cancellationToken);

        await FaseCargaDwhAsync(cancellationToken);

        swGlobal.Stop();
        _logger.LogInfo($"║ CICLO TOTAL COMPLETADO EN: {swGlobal.Elapsed.Minutes}m {swGlobal.Elapsed.Seconds}s");
        _logger.LogInfo("╚══════════════════════════════════════════════════╝");
    }

    private async Task FaseExtraccionAsync(CancellationToken ct)
    {
        _logger.LogInfo(">>> Iniciando Fase de Extracción a Staging...");

        // CSV
        await ExtraerYPersistirAsync(_csvExtractor, d => d.Select(e => new OpinionExtraidaDto
        {
            CodigoOriginal = e.IdOpinion.ToString(),
            IdCliente = e.IdCliente.ToString(),
            IdProducto = e.IdProducto.ToString(),
            Fecha = e.Fecha.ToString("yyyy-MM-dd"),
            Comentario = e.Comentario?.Trim() ?? string.Empty,
            FuenteOrigen = "CSV",
            PuntajeSatisfaccion = e.PuntajeSatisfaccion
        }), m => _staging.GuardarEncuestasAsync(m), ct);

        // DATABASE
        await ExtraerYPersistirAsync(_dbExtractor, d => d.Select(r => new OpinionExtraidaDto
        {
            CodigoOriginal = r.idOpinionGlobal.ToString(),
            FuenteOrigen = "DATABASE",
            Rating = r.Rating
        }), m => _staging.GuardarResenasAsync(m), ct);

        // API
        await ExtraerYPersistirAsync(_apiExtractor, d => d.Select(c => new OpinionExtraidaDto
        {
            CodigoOriginal = c.IdComentario,
            IdCliente = c.IdCliente,
            IdProducto = c.IdProducto,
            Fecha = c.Fecha,
            Comentario = c.Comentario,
            RedSocial = c.RedSocial,
            FuenteOrigen = "API"
        }), m => _staging.GuardarComentariosSocialesAsync(m), ct);
    }

    private async Task FaseCargaDwhAsync(CancellationToken ct)
    {
        _logger.LogInfo(">>> Iniciando Fase de Carga al Data Warehouse...");

        var dataConsolidada = (await _staging.ObtenerTodaLaDataStagingAsync(ct)).ToArray();

        if (dataConsolidada.Length == 0)
        {
            _logger.LogWarning("No hay datos en Staging para cargar al DWH.");
            return;
        }

        try
        {
          
            var tareasDimensiones = _dimLoaders
                .Select(loader => loader.LoadAsync(dataConsolidada, ct));

            await Task.WhenAll(tareasDimensiones);
            _logger.LogInfo("✓ Dimensiones actualizadas.");

            await _factLoader.LoadFactAsync(dataConsolidada, ct);
            _logger.LogInfo("✓ Tabla de Hechos cargada.");
        }
        catch (Exception ex)
        {
            _logger.LogError("✗ ERROR CRÍTICO en la carga al DWH: " + ex.Message, ex);
        }
    }

    // --- Helper genérico (Corregido para retornar métricas) ---
    private async Task<(int registros, int errores)> ExtraerYPersistirAsync<TResult>(
        IExtractor<TResult> extractor,
        Func<IEnumerable<TResult>, IEnumerable<OpinionExtraidaDto>> mapear,
        Func<IEnumerable<OpinionExtraidaDto>, Task> persistir,
        CancellationToken ct) where TResult : class
    {
        try
        {
            var datos = (await extractor.ExtractAsync(ct)).ToList();
            var mapped = mapear(datos).ToList();
            await persistir(mapped);
            _logger.LogInfo($"── [{extractor.NombreFuente}] ✓ {mapped.Count} registros extraídos.");
            return (mapped.Count, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError($"── [{extractor.NombreFuente}] ✗ Error: {ex.Message}");
            return (0, 1);
        }
    }
}