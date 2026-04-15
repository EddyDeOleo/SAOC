

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public class ClasificacionLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _context;
        public ClasificacionLoader(DWHOpinionesClientesContext_ETL context) => _context = context;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            await _context.Clasificacion.ExecuteDeleteAsync(ct);

            var set = new HashSet<string>();
            var items = data
                .Where(x => !string.IsNullOrEmpty(x.Clasificacion) && set.Add(x.Clasificacion))
                .Select(x => new Clasificacion
                {
                    idClasificacion = x.Clasificacion.GetHashCode(),
                    Etiqueta = x.Clasificacion,
                    EsPositiva = x.PuntajeSatisfaccion >= 4 
                }).ToArray();

            await _context.Clasificacion.AddRangeAsync(items, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
