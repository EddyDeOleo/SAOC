using CargaDeEncuestasInternas.Service;

namespace AnalisisComentariosWeb.WKService
{

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "╔══════════════════════════════════════════╗");
            _logger.LogInformation(
                "║  Worker ETL iniciado: {Time}             ║", DateTimeOffset.Now);
            _logger.LogInformation(
                "╚══════════════════════════════════════════╝");

            int intervaloMinutos = _config.GetValue<int>(
                "WorkerSettings:IntervaloMinutos", 60);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "=== Ciclo iniciado: {Time} ===", DateTimeOffset.Now);

                try
                {
                    // Scope por ciclo — DbContext y repositorios Scoped
                    // se instancian y liberan correctamente en cada ejecución
                    using var scope = _scopeFactory.CreateScope();
                    var orchestrator = scope.ServiceProvider
                        .GetRequiredService<EtlOrchestrator>();

                    await orchestrator.EjecutarEtlCompletoAsync(stoppingToken);

                    _logger.LogInformation(
                        "=== Ciclo completado: {Time} | Próximo en {Min} min ===",
                        DateTimeOffset.Now, intervaloMinutos);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Worker detenido por cancelación.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error crítico en ciclo ETL: {Mensaje}", ex.Message);
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(intervaloMinutos), stoppingToken);
            }

            _logger.LogInformation(
                "Worker ETL detenido: {Time}", DateTimeOffset.Now);
        }

    }
}
