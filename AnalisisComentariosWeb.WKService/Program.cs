using AnalisisComentariosWeb.WKService;
using CargaDeEncuestasInternas.Entities.API;
using CargaDeEncuestasInternas.Entities.Csv;
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Service.Loaders.Dim; 
using CargaDeEncuestasInternas.Models.dboSchema;
using CargaDeEncuestasInternas.Models.DTOs;
using CargaDeEncuestasInternas.Persistence.Repositories.API;
using CargaDeEncuestasInternas.Persistence.Repositories.Csv;
using CargaDeEncuestasInternas.Persistence.Repositories.dbo;
using CargaDeEncuestasInternas.Persistence.Repositories.Dwh;
using CargaDeEncuestasInternas.Service;
using CargaDeEncuestasInternas.Service.Loaders.Fact;
using Microsoft.EntityFrameworkCore;
using EncuestasInternas.Entities;
using CargaDeEncuestasInternas.Models.DwhSchema.Entities;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

// ── Logging ──────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ── DbContext del DWH ────────────────────────────────────────
builder.Services.AddDbContext<DWHOpinionesClientesContext_ETL>(options =>
    options.UseSqlServer(config.GetConnectionString("DwhConnection")),
    ServiceLifetime.Scoped);

builder.Services.AddDbContext<DWHOpinionesClientesContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DwhConnection")),
    ServiceLifetime.Scoped);

// ── HTTP Client para ApiExtractor ────────────────────────────
builder.Services.AddHttpClient("ComentariosApi", client =>
{
    client.BaseAddress = new Uri(
        config["ApiSettings:ComentariosUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Logger y Repositorio ─────────────────────────────────────
builder.Services.AddScoped<ILoggerService, LoggerService>();
builder.Services.AddScoped<IStagingRepository, StagingRepository>();

builder.Services.AddScoped<IExtractor<Encuesta>, CsvExtractor>();
builder.Services.AddScoped<IExtractor<Resena>, DatabaseExtractor>();
builder.Services.AddScoped<IExtractor<ComentarioSocialDto>, ApiExtractor>();

builder.Services.AddScoped<IDimensionLoader<OpinionExtraidaDto>, TiempoLoader>();
builder.Services.AddScoped<IDimensionLoader<OpinionExtraidaDto>, ProductoLoader>();
builder.Services.AddScoped<IDimensionLoader<OpinionExtraidaDto>, ClienteLoader>();
builder.Services.AddScoped<IDimensionLoader<OpinionExtraidaDto>, CanalLoader>();
builder.Services.AddScoped<IDimensionLoader<OpinionExtraidaDto>, ClasificacionLoader>();
builder.Services.AddScoped<IDimensionLoader<OpinionExtraidaDto>, TipoOpinionLoader>();

// Registro del Cargador de Hechos
builder.Services.AddScoped<IFactLoader<OpinionExtraidaDto>, OpinionesFactLoader>();

// ── Orquestador ──────────────────────────────────────────────
builder.Services.AddScoped<EtlOrchestrator>();

// ── Worker ───────────────────────────────────────────────────
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();