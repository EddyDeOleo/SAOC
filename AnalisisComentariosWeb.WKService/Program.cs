

using AnalisisComentariosWeb.WKService;
using CargaDeEncuestasInternas.Entities.API;
using CargaDeEncuestasInternas.Entities.Csv;
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.dboSchema;
using CargaDeEncuestasInternas.Models.DwhSchema.Entities;
using CargaDeEncuestasInternas.Persistence.Repositories.API;
using CargaDeEncuestasInternas.Persistence.Repositories.Csv;
using CargaDeEncuestasInternas.Persistence.Repositories.dbo;
using CargaDeEncuestasInternas.Persistence.Repositories.Dwh;
using CargaDeEncuestasInternas.Service;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

// ── Logging ──────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ── DbContext del DWH — apunta al contexto de Stg ────────────
// Namespace: CargaDeEncuestasInternas.Models.DwhSchema.Entities
builder.Services.AddDbContext<DWHOpinionesClientesContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DwhConnection")),
    ServiceLifetime.Scoped);

// ── HTTP Client para ApiExtractor ────────────────────────────
// Seguridad: URL centralizada en appsettings.json
builder.Services.AddHttpClient("ComentariosApi", client =>
{
    client.BaseAddress = new Uri(
        config["ApiSettings:ComentariosUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Logger Service ───────────────────────────────────────────
builder.Services.AddScoped<ILoggerService, LoggerService>();

// ── Repositorio de Staging ───────────────────────────────────
builder.Services.AddScoped<IStagingRepository, StagingRepository>();

// ── Extractores tipados ──────────────────────────────────────
// Escalabilidad: agregar nueva fuente = nuevo IExtractor<T> + una línea aquí
builder.Services.AddScoped<IExtractor<Encuesta>, CsvExtractor>();
builder.Services.AddScoped<IExtractor<Resena>, DatabaseExtractor>();
builder.Services.AddScoped<IExtractor<ComentarioSocialDto>, ApiExtractor>();

// ── Orquestador ──────────────────────────────────────────────
builder.Services.AddScoped<EtlOrchestrator>();

// ── Worker (BackgroundService) ───────────────────────────────
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();