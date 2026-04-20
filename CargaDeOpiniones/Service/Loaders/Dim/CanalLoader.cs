
using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EFCore.BulkExtensions;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.Data.SqlClient;

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

            var bulkConfig = new BulkConfig { SqlBulkCopyOptions = SqlBulkCopyOptions.KeepIdentity };
            await _db.BulkInsertAsync(entidades, bulkConfig, cancellationToken: ct);
        }
    }
}
