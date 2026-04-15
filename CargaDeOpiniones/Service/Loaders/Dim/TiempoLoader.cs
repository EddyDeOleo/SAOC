using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using System.Globalization;
using EFCore.BulkExtensions;


namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public sealed class TiempoLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        private static readonly string[] _formatos =
            ["yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd"];

        public TiempoLoader(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            var fechasUnicas = new HashSet<DateOnly>(
                data
                    .Where(d => !string.IsNullOrWhiteSpace(d.Fecha))
                    .Select(d =>
                    {
                        var pudo = DateTime.TryParseExact(
                            d.Fecha, _formatos,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var dt);

                        if (!pudo) pudo = DateTime.TryParse(d.Fecha, out dt);

                        return pudo ? DateOnly.FromDateTime(dt) : DateOnly.MinValue;
                    })
                    .Where(f => f != DateOnly.MinValue)   // descarta fechas inválidas
            );

            var cultura = new CultureInfo("es-ES");

            var entidades = fechasUnicas
                .Select(fecha =>
                {
                    var diaSemana = (int)fecha.DayOfWeek;  
                    return new Tiempo
                    {
                        Fecha = fecha,
                        Anio = (short)fecha.Year,
                        Trimestre = (byte)((fecha.Month - 1) / 3 + 1),
                        Mes = (byte)fecha.Month,
                        NombreMes = fecha.ToString("MMMM", cultura),
                        Semana = (byte)ISOWeek.GetWeekOfYear(fecha.ToDateTime(TimeOnly.MinValue)),
                        DiaSemana = (byte)diaSemana,
                        NombreDia = fecha.ToString("dddd", cultura),
                        EsFinDeSemana = diaSemana is 0 or 6
                    };
                })
                .ToArray();

            await _db.BulkInsertAsync(entidades, cancellationToken: ct);
        }
    }
}