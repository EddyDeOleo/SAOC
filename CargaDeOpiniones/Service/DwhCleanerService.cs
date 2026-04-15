using EncuestasInternas.Entities;
using Microsoft.EntityFrameworkCore;


namespace CargaDeEncuestasInternas.Service
{
    public sealed class DwhCleanerService
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        public DwhCleanerService(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LimpiarAsync(CancellationToken ct = default)
        {
            await _db.Opiniones.ExecuteDeleteAsync(ct);

            await _db.Tiempo.ExecuteDeleteAsync(ct);
            await _db.Cliente.ExecuteDeleteAsync(ct);
            await _db.Producto.ExecuteDeleteAsync(ct);

            await _db.Canal.ExecuteDeleteAsync(ct);
            await _db.Clasificacion.ExecuteDeleteAsync(ct);
            await _db.TipoOpinion.ExecuteDeleteAsync(ct);
        }
    }
}
