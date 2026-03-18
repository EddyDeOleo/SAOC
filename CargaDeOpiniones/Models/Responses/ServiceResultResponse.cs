

using CargaDeEncuestasInternas.Entities.API;

namespace CargaDeEncuestasInternas.Models.Responses
{
    // Modelo interno para deserializar el wrapper ServiceResult<T>
    public class ServiceResultResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ComentarioSocialDto> Data { get; set; } = [];
    }
}
