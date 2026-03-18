

using CargaDeEncuestasInternas.Interfaces;
using Microsoft.Extensions.Logging;

namespace CargaDeEncuestasInternas.Service
{

    /// <summary>
    /// Implementación de ILoggerService.
    /// Envuelve ILogger de .NET para centralizar el formato
    /// de los mensajes del pipeline ETL.
    /// Mantenibilidad: cambiar a Serilog solo requiere modificar
    ///                 esta clase sin tocar extractores ni repositorios.
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;

        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
        }

        public void LogInfo(string mensaje) =>
            _logger.LogInformation("{Mensaje}", mensaje);

        public void LogWarning(string mensaje) =>
            _logger.LogWarning("{Mensaje}", mensaje);

        public void LogError(string mensaje, Exception? ex = null)
        {
            if (ex is not null)
                _logger.LogError(ex, "{Mensaje}", mensaje);
            else
                _logger.LogError("{Mensaje}", mensaje);
        }
    }
}
