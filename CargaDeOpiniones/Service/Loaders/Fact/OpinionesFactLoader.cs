using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.FactSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Service.Loaders.Fact
{
    public class OpinionesFactLoader : IFactLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _context;
        public OpinionesFactLoader(DWHOpinionesClientesContext_ETL context) => _context = context;

        public async Task LoadFactAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            await _context.Opiniones.ExecuteDeleteAsync(ct);

            var hechos = data
                .Where(x => !string.IsNullOrWhiteSpace(x.Fecha)) 
                .Select(x =>
                {
                    bool esFechaValida = DateTime.TryParse(x.Fecha, out DateTime fechaCasteada);
                    return new { x, fechaCasteada, esFechaValida };
                })
                .Where(temp => temp.esFechaValida) 
                .Select(temp => new Opiniones
                {
                    CodigoOriginal = temp.x.CodigoOriginal,
                    idTiempo = int.Parse(temp.fechaCasteada.ToString("yyyyMMdd")),

                    idCliente = int.TryParse(temp.x.IdCliente, out int idC) ? idC : 0,
                    idProducto = int.TryParse(temp.x.IdProducto, out int idP) ? idP : 0,

                    PuntajeSatisfaccion = (byte?)(temp.x.PuntajeSatisfaccion ?? 0),
                    Rating = (byte?)(temp.x.Rating ?? 0),

                    EsPositiva = (byte)(temp.x.PuntajeSatisfaccion >= 4 || temp.x.Rating >= 4 ? 1 : 0),
                    EsNegativa = (byte)(temp.x.PuntajeSatisfaccion <= 2 || temp.x.Rating <= 2 ? 1 : 0),
                    EsNeutra = (byte)(temp.x.PuntajeSatisfaccion == 3 || temp.x.Rating == 3 ? 1 : 0),

                    ConteoOpinion = 1,
                    idCanal = 1, 
                    idClasificacion = 1,
                    idTipoOpinion = 1
                }).ToArray();

            if (hechos.Any())
            {
                await _context.Opiniones.AddRangeAsync(hechos, ct);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}