

using AnalisisComentariosWeb.WKService;
using CargaDeEncuestasInternas.Entities.API;
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Persistence.Repositories.API;
using CargaDeEncuestasInternas.Persistence.Repositories.Csv;
using CargaDeEncuestasInternas.Persistence.Repositories.dbo;
using CargaDeEncuestasInternas.Persistence.Repositories.Dwh;
using CargaDeEncuestasInternas.Service;
using EncuestasInternas.Entities;
using Microsoft.EntityFrameworkCore;
using Encuesta = CargaDeEncuestasInternas.Entities.Csv.Encuesta;
using Resena = CargaDeEncuestasInternas.Models.dboSchema.Resena;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ── DbContext del DWH (tablas Stg.*) ─────────────────────────
builder.Services.AddDbContext<DWHOpinionesClientesContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DwhConnection")),
    ServiceLifetime.Scoped);

// ── HTTP Client — URL centralizada en appsettings.json ───────
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
builder.Services.AddScoped<IExtractor<Encuesta>, CsvExtractor>();
builder.Services.AddScoped<IExtractor<Resena>, DatabaseExtractor>();
builder.Services.AddScoped<IExtractor<ComentarioSocialDto>, ApiExtractor>();

// ── Orquestador ──────────────────────────────────────────────
builder.Services.AddScoped<EtlOrchestrator>();

// ── Worker ───────────────────────────────────────────────────
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();