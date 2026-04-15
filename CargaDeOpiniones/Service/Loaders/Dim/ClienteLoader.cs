

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.EntityFrameworkCore;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public class ClienteLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _context;
        public ClienteLoader(DWHOpinionesClientesContext_ETL context) => _context = context;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            await _context.Cliente.ExecuteDeleteAsync(ct);

            var hashSetClientes = new HashSet<string>();

            var clientesParaCargar = data
                .Where(x => hashSetClientes.Add(x.IdCliente)) 
                .Select(x => new Cliente
                {
                    idCliente = int.Parse(x.IdCliente),
                    NombreCliente = "Cliente Desconocido", 
                    Email = "sin-email@empresa.com",
                    Pais = "República Dominicana",
                    Segmento = "General"
                }).ToArray();

            await _context.Cliente.AddRangeAsync(clientesParaCargar, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
