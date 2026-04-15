

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public class TiempoLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _context;

        public TiempoLoader(DWHOpinionesClientesContext_ETL context) => _context = context;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            await _context.Tiempo.ExecuteDeleteAsync(ct);

            var fechasUnicas = data
                .Select(x => DateTime.Parse(x.Fecha))
                .Distinct()
                .ToArray();

            var registrosTiempo = fechasUnicas.Select(fecha => new Tiempo
            {
                idTiempo = int.Parse(fecha.ToString("yyyyMMdd")),
                Fecha = DateOnly.FromDateTime(fecha),
                Anio = (short)fecha.Year,
                Trimestre = (byte)((fecha.Month + 2) / 3),
                Mes = (byte)fecha.Month,
                NombreMes = fecha.ToString("MMMM"),
                Semana = (byte)((fecha.DayOfYear / 7) + 1),
                DiaSemana = (byte)fecha.DayOfWeek,
                NombreDia = fecha.ToString("dddd"),
                EsFinDeSemana = fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday
            }).ToArray();

            await _context.Tiempo.AddRangeAsync(registrosTiempo, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
