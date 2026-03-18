using CargaDeEncuestasInternas.Entities.Csv;
using CargaDeEncuestasInternas.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Globalization;


namespace CargaDeEncuestasInternas.Persistence.Repositories.Csv
{

    /// <summary>
    /// Extrae encuestas internas desde archivos CSV.
    /// Rendimiento: Task.Run evita bloquear el hilo del Worker
    ///              ya que CsvHelper es síncrono internamente.
    /// </summary>
    public class CsvExtractor : IExtractor<Encuesta>
    {
        public string NombreFuente => "CSV";

        private readonly IConfiguration _config;
        private readonly ILoggerService _logger;

        public CsvExtractor(IConfiguration config, ILoggerService logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<IEnumerable<Encuesta>> ExtractAsync(
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var ruta = _config["CsvSettings:PathFileEncuestas"]
                ?? throw new InvalidOperationException(
                    "CsvSettings:PathFileEncuestas no configurado en appsettings.json");

            _logger.LogInfo($"[{NombreFuente}] Iniciando extracción desde: {ruta}");

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header
                    .ToLower()
                    .Replace("ó", "o")
                    .Replace("á", "a")
                    .Trim(),
                HeaderValidated = null,
                MissingFieldFound = null
            };

            var resultado = await Task.Run(() =>
            {
                using var reader = new StreamReader(ruta);
                using var csv = new CsvReader(reader, csvConfig);
                return csv.GetRecords<Encuesta>().ToList();
            }, cancellationToken);

            sw.Stop();
            _logger.LogInfo(
                $"[{NombreFuente}] {resultado.Count} registros extraídos en {sw.ElapsedMilliseconds}ms");

            return resultado;
        }
    }
}
