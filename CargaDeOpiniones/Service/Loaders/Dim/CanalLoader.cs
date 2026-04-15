

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public class CanalLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _context;
        public CanalLoader(DWHOpinionesClientesContext_ETL context) => _context = context;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            await _context.Canal.ExecuteDeleteAsync(ct);

            var fuentesUnicas = new HashSet<string>();
            var canales = data
                .Where(x => fuentesUnicas.Add(x.FuenteOrigen))
                .Select(x => new Canal
                {
                    NombreCanal = x.FuenteOrigen,
                    TipoCanal = x.RedSocial ?? "Interno"
                }).ToArray();

            await _context.Canal.AddRangeAsync(canales, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
