
using AnalisisComentariosWeb.API.Models;
using CargaDeEncuestasInternas.Models.Responses;

namespace CargaDeEncuestasInternas.Interfaces.API
{
    public interface IComentariosSocialesService
    {
        Task<ServiceResult<IEnumerable<ComentarioSocialResponse>>>
            GetAllAsync(CancellationToken cancellationToken);

        Task<ServiceResult<IEnumerable<ComentarioSocialResponse>>>
            GetByRedSocialAsync(string redSocial, CancellationToken cancellationToken);
    }

}
