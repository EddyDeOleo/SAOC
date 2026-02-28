
namespace CargaDeEncuestasInternas.Models.Csv
{
    public class ComentarioSocial
    {
        public string IdComment { get; set; } = string.Empty;  // "T0001" → CodigoOriginal
        public string IdCliente { get; set; } = string.Empty;  // "C019"  → parsear a 19
        public string IdProducto { get; set; } = string.Empty;  // "P003"  → parsear a 3
        public string Fuente { get; set; } = string.Empty;  // "Instagram", "Twitter", etc.
        public string Fecha { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
    }
}
