

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using EFCore.BulkExtensions;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public sealed class ClienteLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        public ClienteLoader(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            var idsUnicos = new HashSet<int>(
                data
                    .Where(d => !string.IsNullOrWhiteSpace(d.IdCliente)
                             && int.TryParse(d.IdCliente, out _))
                    .Select(d => int.Parse(d.IdCliente))
            );

            var entidades = idsUnicos
                .Select(id => new Cliente
                {
                    idCliente = id,
                    NombreCliente = $"Cliente_{id}",
                    Email = string.Empty,
                    Pais = "N/D",
                    Segmento = "N/D"
                })
                .ToArray();

            await _db.BulkInsertAsync(entidades, cancellationToken: ct);
        }
    }
}
