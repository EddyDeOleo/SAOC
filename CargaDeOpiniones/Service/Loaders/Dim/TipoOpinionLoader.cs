

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EFCore.BulkExtensions;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using Microsoft.Data.SqlClient;


namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public sealed class TipoOpinionLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        private static readonly TipoOpinion[] _semilla =
        [
            new() { idTipoOpinion = 1, Descripcion = "Encuesta"          },
        new() { idTipoOpinion = 2, Descripcion = "Reseña"            },
        new() { idTipoOpinion = 3, Descripcion = "Comentario Social" }
        ];

        public TipoOpinionLoader(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            var entidades = _semilla
                .Select(t => new TipoOpinion
                {
                    idTipoOpinion = t.idTipoOpinion,
                    Descripcion = t.Descripcion
                })
                .ToArray();

            var bulkConfig = new BulkConfig { SqlBulkCopyOptions = SqlBulkCopyOptions.KeepIdentity };
            await _db.BulkInsertAsync(entidades, bulkConfig, cancellationToken: ct);
        }
    }
}
