

using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.DimSchema;
using EFCore.BulkExtensions;

namespace CargaDeEncuestasInternas.Service.Loaders.Dim
{
    public sealed class ClasificacionLoader : IDimensionLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        private static readonly Clasificacion[] _semilla =
        [
            new() { idClasificacion = 1, Etiqueta = "Positiva", EsPositiva = true  },
        new() { idClasificacion = 2, Etiqueta = "Negativa", EsPositiva = false },
        new() { idClasificacion = 3, Etiqueta = "Neutra",   EsPositiva = false }
        ];

        public ClasificacionLoader(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LoadAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
            var entidades = _semilla
                .Select(c => new Clasificacion
                {
                    idClasificacion = c.idClasificacion,
                    Etiqueta = c.Etiqueta,
                    EsPositiva = c.EsPositiva
                })
                .ToArray();

            await _db.BulkInsertAsync(entidades, cancellationToken: ct);
        }
    }
}
