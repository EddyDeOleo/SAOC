using AnalisisComentariosWeb.API.Models;
using CargaDeEncuestasInternas.Interfaces.API;
using CargaDeEncuestasInternas.Models;
using CargaDeEncuestasInternas.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace CargaDeEncuestasInternas.Service.API
{

    public class ComentariosSocialesService : IComentariosSocialesService
    {
        private readonly OpinionesClientesContext _db;
        private readonly ILogger<ComentariosSocialesService> _logger;

        public ComentariosSocialesService(
            OpinionesClientesContext db,
            ILogger<ComentariosSocialesService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ServiceResult<IEnumerable<ComentarioSocialResponse>>>
            GetAllAsync(CancellationToken cancellationToken)
        {
            try
            {
                var data = await ObtenerQueryBase()
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "[Service] {Count} comentarios sociales obtenidos", data.Count);

                return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                    .Ok(data, $"{data.Count} comentarios obtenidos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Service] Error al obtener comentarios sociales: {Mensaje}", ex.Message);
                return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                    .Fail("Error al obtener los comentarios sociales.");
            }
        }

        public async Task<ServiceResult<IEnumerable<ComentarioSocialResponse>>>
            GetByRedSocialAsync(string redSocial, CancellationToken cancellationToken)
        {
            try
            {
                var data = await ObtenerQueryBase()
                    .Where(x => x.RedSocial
                        .Equals(redSocial, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync(cancellationToken);

                if (!data.Any())
                {
                    _logger.LogWarning(
                        "[Service] No se encontraron comentarios para: {RedSocial}",
                        redSocial);
                    return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                        .Fail($"No hay comentarios de '{redSocial}'");
                }

                return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                    .Ok(data, $"{data.Count()} comentarios de {redSocial}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Service] Error al filtrar por red social: {Mensaje}", ex.Message);
                return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                    .Fail("Error al obtener los comentarios por red social.");
            }
        }


        // Query base reutilizable — JOIN: ComentarioSocial → Opiniones → Fuentes → Cat_Canal
        private IQueryable<ComentarioSocialResponse> ObtenerQueryBase() =>
            _db.ComentarioSocial
                .AsNoTracking()
                .Join(_db.Opiniones,
                    cs => cs.idOpinionGlobal,
                    op => op.idOpinionGlobal,
                    (cs, op) => new { cs, op })
                .Join(_db.Fuentes,
                    x => x.op.idFuente,
                    f => f.idFuente,
                    (x, f) => new { x.cs, x.op, f })
                .Join(_db.Cat_Canal,
                    x => x.f.idCanal,
                    cc => cc.idCanal,
                    (x, cc) => new ComentarioSocialResponse
                    {
                        IdComentario = x.op.CodigoOriginal,
                        IdCliente = x.op.idCliente.ToString(),
                        IdProducto = x.op.idProducto.ToString(),
                        RedSocial = cc.NombreCanal,
                        Fecha = x.op.Fecha.ToString("yyyy-MM-dd"),
                        Comentario = x.op.Comentario
                    });
    }
}
