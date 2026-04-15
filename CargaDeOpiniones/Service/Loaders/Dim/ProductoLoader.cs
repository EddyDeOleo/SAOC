using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public class ProductoLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _context;

        public ProductoLoader(DWHOpinionesClientesContext_ETL context)
        {
            _context = context;
        }

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            await _context.Producto.ExecuteDeleteAsync(ct);

            var uniqueProducts = new HashSet<string>(
                data.Select(x => x.IdProducto).Where(id => !string.IsNullOrEmpty(id))
            );

            var productosParaCargar = uniqueProducts
                .Select(id => new Producto
                {
                    idProducto = int.Parse(id),
                    NombreProducto = $"Producto {id}", 
                    Categoria = "General"
                })
                .ToArray();

            if (productosParaCargar.Any())
            {
                await _context.Producto.AddRangeAsync(productosParaCargar, ct);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
