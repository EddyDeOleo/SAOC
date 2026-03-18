

using CargaDeEncuestasInternas.Entities.API;
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using System.Diagnostics;
using Encuesta = CargaDeEncuestasInternas.Entities.Csv.Encuesta;
using Resena = CargaDeEncuestasInternas.Models.dboSchema.Resena;


namespace CargaDeEncuestasInternas.Service
{

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
            _logger.LogInfo("EtlOrchestrator: iniciando ciclo de extracción");

            await ExtraerYPersistirAsync(
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

            await ExtraerYPersistirAsync(
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

            await ExtraerYPersistirAsync(
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

            _logger.LogInfo("EtlOrchestrator: ciclo de extracción finalizado");
        }

        private async Task ExtraerYPersistirAsync<TResult>(
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
                    $"── [{extractor.NombreFuente}] {mapped.Count} registros en {sw.ElapsedMilliseconds}ms");

                await _staging.RegistrarLogAsync(
                    extractor.NombreFuente, mapped.Count, 0, "COMPLETADO");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(
                    $"── [{extractor.NombreFuente}] ERROR: {ex.Message} ({sw.ElapsedMilliseconds}ms)", ex);

                await _staging.RegistrarLogAsync(
                    extractor.NombreFuente, 0, 1, "ERROR", ex.Message);
            }
        }
    }
}
