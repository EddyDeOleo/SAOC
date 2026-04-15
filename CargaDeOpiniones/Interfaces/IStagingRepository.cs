

using CargaDeEncuestasInternas.Models.DTOs;

namespace CargaDeEncuestasInternas.Interfaces
{
    public interface IStagingRepository
    {
        Task GuardarEncuestasAsync(IEnumerable<OpinionExtraidaDto> datos);
        Task GuardarResenasAsync(IEnumerable<OpinionExtraidaDto> datos);
        Task GuardarComentariosSocialesAsync(IEnumerable<OpinionExtraidaDto> datos);
        Task RegistrarLogAsync(
            string fuente,
            int registros,
            int errores,
            string estado,
            string? mensajeError = null);

        Task<IEnumerable<OpinionExtraidaDto>> ObtenerTodaLaDataStagingAsync(CancellationToken ct = default);
    }


}
