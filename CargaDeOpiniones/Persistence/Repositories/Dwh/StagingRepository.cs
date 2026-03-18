

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;

namespace CargaDeEncuestasInternas.Persistence.Repositories.Dwh
{

    /// <summary>
    /// Persiste los datos extraídos en las tablas Stg.* del DWH.
    /// Usa DWHOpinionesClientesContext (ya existente en el proyecto).
    /// </summary>
    public class StagingRepository : IStagingRepository
    {
        private readonly DWHOpinionesClientesContext _db;
        private readonly ILoggerService _logger;

        public StagingRepository(
            DWHOpinionesClientesContext db,
            ILoggerService logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task GuardarEncuestasAsync(IEnumerable<OpinionExtraidaDto> datos)
        {
            var entidades = datos.Select(d => new StgEncuesta
            {
                IdOpinion = d.CodigoOriginal,
                IdCliente = d.IdCliente,
                IdProducto = d.IdProducto,
                Fecha = d.Fecha,
                Comentario = d.Comentario,
                Clasificacion = d.Clasificacion,
                PuntajeSatisfaccion = d.PuntajeSatisfaccion?.ToString(),
                FechaExtraccion = DateTime.Now,
                FuenteOrigen = "CSV",
                EstadoProcesamiento = "PENDIENTE"
            }).ToList();

            await _db.StgEncuestas.AddRangeAsync(entidades);
            await _db.SaveChangesAsync();

            _logger.LogInfo(
                $"[Staging] {entidades.Count} encuestas guardadas en Stg.Encuestas");
        }

        public async Task GuardarResenasAsync(IEnumerable<OpinionExtraidaDto> datos)
        {
            var entidades = datos.Select(d => new StgResena
            {
                IdOpinionGlobal = int.TryParse(d.CodigoOriginal, out var og) ? og : 0,
                IdCliente = int.TryParse(d.IdCliente, out var c) ? c : 0,
                IdProducto = int.TryParse(d.IdProducto, out var p) ? p : 0,
                Fecha = d.Fecha,
                Comentario = d.Comentario,
                Rating = d.Rating.HasValue ? (byte?)d.Rating.Value : null,
                FechaExtraccion = DateTime.Now,
                FuenteOrigen = "DATABASE",
                EstadoProcesamiento = "PENDIENTE"
            }).ToList();

            await _db.StgResenas.AddRangeAsync(entidades);
            await _db.SaveChangesAsync();

            _logger.LogInfo(
                $"[Staging] {entidades.Count} reseñas guardadas en Stg.Resenas");
        }

        public async Task GuardarComentariosSocialesAsync(
            IEnumerable<OpinionExtraidaDto> datos)
        {
            var entidades = datos.Select(d => new StgComentarioSocial
            {
                IdComentario = d.CodigoOriginal,
                IdCliente = d.IdCliente,
                IdProducto = d.IdProducto,
                RedSocial = d.RedSocial,
                Fecha = d.Fecha,
                Comentario = d.Comentario,
                FechaExtraccion = DateTime.Now,
                FuenteOrigen = "API",
                EstadoProcesamiento = "PENDIENTE"
            }).ToList();

            await _db.StgComentariosSociales.AddRangeAsync(entidades);
            await _db.SaveChangesAsync();

            _logger.LogInfo(
                $"[Staging] {entidades.Count} comentarios guardados en Stg.ComentariosSociales");
        }

        public async Task RegistrarLogAsync(
            string fuente, int registros, int errores,
            string estado, string? mensajeError = null)
        {
            var log = new StgLogExtraccion
            {
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now,
                FuenteOrigen = fuente,
                RegistrosExtraidos = registros,
                RegistrosConError = errores,
                Estado = estado,
                MensajeError = mensajeError
            };

            await _db.StgLogExtraccion.AddAsync(log);
            await _db.SaveChangesAsync();

            _logger.LogInfo(
                $"[Log] {fuente} → {estado}: {registros} registros, {errores} errores");
        }
    }
}
