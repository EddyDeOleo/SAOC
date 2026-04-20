
using Resena = CargaDeEncuestasInternas.Entities.dbo.Resena;

using CargaDeEncuestasInternas.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics;

namespace CargaDeEncuestasInternas.Persistence.Repositories.dbo;

/// <summary>
/// Extrae reseñas web desde la BD relacional OLTP.
/// Rendimiento: ADO.NET + Stored Procedure es más eficiente
///              que EF Core para lecturas masivas — SqlDataReader
///              lee row por row sin materializar entidades en memoria.
/// </summary>
public class DatabaseExtractor : IExtractor<Resena>
{
    public string NombreFuente => "DATABASE";

    private readonly IConfiguration _config;
    private readonly ILoggerService _logger;

    public DatabaseExtractor(IConfiguration config, ILoggerService logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<IEnumerable<Resena>> ExtractAsync(
     CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var connString = _config.GetConnectionString("OltpConnection")
            ?? throw new InvalidOperationException(
                "OltpConnection no configurado en appsettings.json");

        _logger.LogInfo($"[{NombreFuente}] Ejecutando sp_ObtenerResenas...");

        var resultado = new List<Resena>();

        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("sp_ObtenerResenas", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var codigoRaw = reader["CodigoOriginal"].ToString()!;
            var soloNumeros = new string(codigoRaw.Where(char.IsDigit).ToArray());

            resultado.Add(new Resena
            {
                idOpinionGlobal = int.Parse(soloNumeros),
                CodigoOriginal = codigoRaw,
                idCliente = Convert.ToInt32(reader["idCliente"]),
                idProducto = Convert.ToInt32(reader["idProducto"]),
                Fecha = reader["Fecha"].ToString()!,
                Comentario = reader["Comentario"].ToString() ?? string.Empty,
                Rating = Convert.ToByte(reader["Rating"])
            });
        }

        sw.Stop();
        _logger.LogInfo(
            $"[{NombreFuente}] {resultado.Count:N0} reseñas extraídas en {sw.ElapsedMilliseconds:N0}ms");

        return resultado;
    }
}
