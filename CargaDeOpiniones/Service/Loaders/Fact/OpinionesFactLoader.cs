

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

            var hechos = data.Select(x => new Opiniones
            {
                CodigoOriginal = x.CodigoOriginal,
                idTiempo = int.Parse(DateTime.Parse(x.Fecha).ToString("yyyyMMdd")),
                idCliente = int.Parse(x.IdCliente),
                idProducto = int.Parse(x.IdProducto),
                PuntajeSatisfaccion = (byte?)(x.PuntajeSatisfaccion ?? 0),
                Rating = (byte?)(x.Rating ?? 0),
                EsPositiva = (byte)(x.PuntajeSatisfaccion >= 4 || x.Rating >= 4 ? 1 : 0),
                EsNegativa = (byte)(x.PuntajeSatisfaccion <= 2 || x.Rating <= 2 ? 1 : 0),
                EsNeutra = (byte)(x.PuntajeSatisfaccion == 3 || x.Rating == 3 ? 1 : 0),
                ConteoOpinion = 1,
                idCanal = 1,
                idClasificacion = 1,
                idTipoOpinion = 1
            }).ToArray();

            await _context.Opiniones.AddRangeAsync(hechos, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
