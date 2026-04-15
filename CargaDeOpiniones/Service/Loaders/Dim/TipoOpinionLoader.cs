

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using CargaDeEncuestasInternas.Models.DwhSchema.Entities;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public class TipoOpinionLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _context;
        public TipoOpinionLoader(DWHOpinionesClientesContext_ETL context) => _context = context;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            await _context.TipoOpinion.ExecuteDeleteAsync(ct);

            var tiposUnicos = new HashSet<string>();
            var tipos = data
                .Where(x => tiposUnicos.Add(x.FuenteOrigen))
                .Select(x => new TipoOpinion
                {
                    Descripcion = x.FuenteOrigen
                }).ToArray();

            await _context.TipoOpinion.AddRangeAsync(tipos, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
