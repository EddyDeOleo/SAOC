

using CargaDeEncuestasInternas.Entities.API;
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.Responses;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;

namespace CargaDeEncuestasInternas.Persistence.Repositories.API
{

    /// <summary>
    /// Extrae comentarios de redes sociales desde la API REST.
    /// Seguridad  : URL centralizada en appsettings.json.
    /// Rendimiento: IHttpClientFactory gestiona el pool de conexiones
    ///              evitando socket exhaustion en ejecuciones repetidas.
    /// </summary>
    public class ApiExtractor : IExtractor<ComentarioSocialDto>
    {
        public string NombreFuente => "API";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILoggerService _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiExtractor(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILoggerService logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<IEnumerable<ComentarioSocialDto>> ExtractAsync(
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var endpoint = _config["ApiSettings:ComentariosEndpoint"]
                           ?? "/api/ComentariosSociales/get-all";

            _logger.LogInfo($"[{NombreFuente}] Consumiendo endpoint: {endpoint}");

            var client = _httpClientFactory.CreateClient("ComentariosApi");
            var response = await client.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Deserializa ServiceResult<List<ComentarioSocialDto>> de la API
            var serviceResult = JsonSerializer.Deserialize<ServiceResultResponse>(
                json, _jsonOptions);

            var resultado = serviceResult?.Data ?? [];

            sw.Stop();
            _logger.LogInfo(
                $"[{NombreFuente}] {resultado.Count} comentarios extraídos en {sw.ElapsedMilliseconds}ms");

            return resultado;
        }

     
    }

}
