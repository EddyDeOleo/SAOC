

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.dboSchema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace CargaDeEncuestasInternas.Persistence.Repositories.dbo
{

    /// <summary>
    /// Extrae reseñas web desde la BD relacional OLTP.
    /// Rendimiento: ADO.NET directo es más eficiente que EF Core
    ///              para lecturas masivas sin necesidad de tracking.
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

            _logger.LogInfo($"[{NombreFuente}] Iniciando extracción de Reseñas Web");

            var resultado = new List<Resena>();

            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync(cancellationToken);

            const string sql = """
            SELECT
                op.CodigoOriginal,
                op.idCliente,
                op.idProducto,
                CONVERT(VARCHAR(10), op.Fecha, 120) AS Fecha,
                op.Comentario,
                r.Rating
            FROM dbo.Opiniones op
            INNER JOIN dbo.Resena r
                ON op.idOpinionGlobal = r.idOpinionGlobal
            """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                resultado.Add(new Resena
                {
                    idOpinionGlobal = Convert.ToInt32(reader["CodigoOriginal"]),
                    Rating = Convert.ToByte(reader["Rating"])
                });
            }

            sw.Stop();
            _logger.LogInfo(
                $"[{NombreFuente}] {resultado.Count} reseñas extraídas en {sw.ElapsedMilliseconds}ms");

            return resultado;
        }
    }
}
