using CargaDeEncuestasInternas.Interfaces.API;
using CargaDeEncuestasInternas.Models;
using CargaDeEncuestasInternas.Service.API;
using Microsoft.EntityFrameworkCore;

namespace AnalisisComentariosWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // DbContext del OLTP — generado por EF Core Power Tools
            builder.Services.AddDbContext<OpinionesClientesContext>(options =>
                options.UseSqlServer(builder.Configuration
                    .GetConnectionString("OltpConnection")));

            // Servicio de comentarios sociales
            builder.Services.AddScoped<IComentariosSocialesService, ComentariosSocialesService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}