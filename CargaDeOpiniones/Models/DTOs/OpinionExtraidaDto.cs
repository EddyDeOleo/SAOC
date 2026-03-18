

namespace CargaDeEncuestasInternas.Models.DTOs
{

    /// <summary>
    /// DTO unificado — estandariza la salida de los 3 extractores.
    /// Campos opcionales según fuente:
    ///   PuntajeSatisfaccion + Clasificacion → CSV
    ///   Rating                              → DATABASE
    ///   RedSocial                           → API
    /// </summary>
    public record OpinionExtraidaDto
    {
        public string CodigoOriginal { get; set; } = string.Empty;
        public string IdCliente { get; set; } = string.Empty;
        public string IdProducto { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
        public string FuenteOrigen { get; set; } = string.Empty;
        public string? Clasificacion { get; set; }
        public int? PuntajeSatisfaccion { get; set; }
        public int? Rating { get; set; }
        public string? RedSocial { get; set; }
    }

}
