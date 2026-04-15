
using EFCore.BulkExtensions;
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public sealed class CanalLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        private static readonly Canal[] _semilla =
        [
            new() { idCanal = 1, NombreCanal = "CSV",      TipoCanal = "Archivo"       },
        new() { idCanal = 2, NombreCanal = "DATABASE", TipoCanal = "Base de Datos" },
        new() { idCanal = 3, NombreCanal = "API",      TipoCanal = "Web"           }
        ];

        public CanalLoader(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            var entidades = _semilla
                .Select(c => new Canal
                {
                    idCanal = c.idCanal,
                    NombreCanal = c.NombreCanal,
                    TipoCanal = c.TipoCanal
                })
                .ToArray();

            await _db.BulkInsertAsync(entidades, cancellationToken: ct);
        }
    }
}
