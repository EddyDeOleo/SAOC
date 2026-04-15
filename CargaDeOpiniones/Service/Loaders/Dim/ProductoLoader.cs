using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using EFCore.BulkExtensions;


namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public sealed class ProductoLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        public ProductoLoader(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            var idsUnicos = new HashSet<int>(
                data
                    .Where(d => !string.IsNullOrWhiteSpace(d.IdProducto)
                             && int.TryParse(d.IdProducto, out _))
                    .Select(d => int.Parse(d.IdProducto))
            );

            var entidades = idsUnicos
                .Select(id => new Producto
                {
                    idProducto = id,
                    NombreProducto = $"Producto_{id}",
                    Categoria = "N/D"
                })
                .ToArray();

            await _db.BulkInsertAsync(entidades, cancellationToken: ct);
        }
    }
}
