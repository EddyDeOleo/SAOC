
using AnalisisComentariosWeb.API.Models;
using CargaDeEncuestasInternas.Interfaces.API;
using CargaDeEncuestasInternas.Models;
using CargaDeEncuestasInternas.Models.Responses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CargaDeEncuestasInternas.Service.API;

/// <summary>
/// Implementación de IComentariosSocialesService.
/// Usa ADO.NET + Stored Procedure para máximo rendimiento
/// en la consulta de comentarios sociales desde el OLTP.
/// </summary>
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
            var data = await ObtenerDesdeSpAsync(cancellationToken);

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
            var todos = await ObtenerDesdeSpAsync(cancellationToken);
            var filtrados = todos
                .Where(c => c.RedSocial.Equals(redSocial, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!filtrados.Any())
            {
                _logger.LogWarning(
                    "[Service] No se encontraron comentarios para: {RedSocial}", redSocial);
                return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                    .Fail($"No hay comentarios de '{redSocial}'");
            }

            return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                .Ok(filtrados, $"{filtrados.Count} comentarios de {redSocial}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Service] Error al filtrar por red social: {Mensaje}", ex.Message);
            return ServiceResult<IEnumerable<ComentarioSocialResponse>>
                .Fail("Error al obtener los comentarios por red social.");
        }
    }

    // ── Stored Procedure: sp_ObtenerComentariosSociales ──────
    // ADO.NET directo para máximo rendimiento en lecturas masivas
    private async Task<List<ComentarioSocialResponse>> ObtenerDesdeSpAsync(
        CancellationToken cancellationToken)
    {
        var resultado = new List<ComentarioSocialResponse>();
        var connString = _db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Cadena de conexión no configurada.");

        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("sp_ObtenerComentariosSociales", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            resultado.Add(new ComentarioSocialResponse
            {
                IdComentario = reader["IdComentario"].ToString()!,
                IdCliente = reader["idCliente"].ToString()!,
                IdProducto = reader["idProducto"].ToString()!,
                RedSocial = reader["RedSocial"].ToString()!,
                Fecha = reader["Fecha"].ToString()!,
                Comentario = reader["Comentario"].ToString()!
            });
        }

        return resultado;
    }
}
