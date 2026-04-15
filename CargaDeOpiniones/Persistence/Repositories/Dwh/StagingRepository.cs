

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using CargaDeEncuestasInternas.Models.DwhSchema.Entities;
using CargaDeEncuestasInternas.Models.DwhSchema.Entities.StgSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Persistence.Repositories.Dwh;

/// <summary>
/// Implementación de IStagingRepository.
/// Persiste los datos extraídos en las tablas Stg.* del DWH.
/// Usa DWHOpinionesClientesContext generado por EF Core Power Tools.
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
        var entidades = datos.Select(d => new Encuestas
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

        await _db.Encuestas.AddRangeAsync(entidades);
        await _db.SaveChangesAsync();

        _logger.LogInfo(
            $"[Staging] {entidades.Count} encuestas guardadas en Stg.Encuestas");
    }

    public async Task GuardarResenasAsync(IEnumerable<OpinionExtraidaDto> datos)
    {
        var entidades = datos.Select(d => new Resenas
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

        await _db.Resenas.AddRangeAsync(entidades);
        await _db.SaveChangesAsync();

        _logger.LogInfo(
            $"[Staging] {entidades.Count} reseñas guardadas en Stg.Resenas");
    }

    public async Task GuardarComentariosSocialesAsync(
        IEnumerable<OpinionExtraidaDto> datos)
    {
        var entidades = datos.Select(d => new ComentariosSociales
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

        await _db.ComentariosSociales.AddRangeAsync(entidades);
        await _db.SaveChangesAsync();

        _logger.LogInfo(
            $"[Staging] {entidades.Count} comentarios guardados en Stg.ComentariosSociales");
    }

    public async Task RegistrarLogAsync(
        string fuente,
        int registros,
        int errores,
        string estado,
        string? mensajeError = null)
    {
        var log = new LogExtraccion
        {
            FechaInicio = DateTime.Now,
            FechaFin = DateTime.Now,
            FuenteOrigen = fuente,
            RegistrosExtraidos = registros,
            RegistrosConError = errores,
            Estado = estado,
            MensajeError = mensajeError
        };

        await _db.LogExtraccion.AddAsync(log);
        await _db.SaveChangesAsync();

        _logger.LogInfo(
            $"[Log] {fuente} → {estado}: {registros} registros, {errores} errores");
    }

    public async Task<IEnumerable<OpinionExtraidaDto>> ObtenerTodaLaDataStagingAsync(CancellationToken ct = default)
    {
        var rawEncuestas = await _db.Encuestas.ToListAsync(ct);

        var encuestas = rawEncuestas.Select(e => {
            int.TryParse(e.PuntajeSatisfaccion, out var p); 
            return new OpinionExtraidaDto
            {
                CodigoOriginal = e.IdOpinion,
                IdCliente = e.IdCliente,
                IdProducto = e.IdProducto,
                Fecha = e.Fecha,
                Comentario = e.Comentario,
                Clasificacion = e.Clasificacion,
                PuntajeSatisfaccion = p > 0 ? p : null, 
                FuenteOrigen = "CSV"
            };
        });

        var resenas = await _db.Resenas
            .Select(r => new OpinionExtraidaDto
            {
                CodigoOriginal = r.IdOpinionGlobal.ToString(),
                IdCliente = r.IdCliente.ToString(),
                IdProducto = r.IdProducto.ToString(),
                Fecha = r.Fecha,
                Comentario = r.Comentario,
                Rating = r.Rating,
                FuenteOrigen = "DATABASE"
            })
            .ToListAsync(ct);

        var sociales = await _db.ComentariosSociales
            .Select(s => new OpinionExtraidaDto
            {
                CodigoOriginal = s.IdComentario,
                IdCliente = s.IdCliente,
                IdProducto = s.IdProducto,
                Fecha = s.Fecha,
                Comentario = s.Comentario,
                RedSocial = s.RedSocial,
                FuenteOrigen = "API"
            })
            .ToListAsync(ct);

        return encuestas.Concat(resenas).Concat(sociales).ToArray();
    }
}