
namespace CargaDeEncuestasInternas.Models.Csv
{
    public class Resena
    {
        public string IdReview { get; set; } = string.Empty;  // "W0001" → CodigoOriginal
        public string IdCliente { get; set; } = string.Empty;  // "C007"  → parsear a 7
        public string IdProducto { get; set; } = string.Empty;  // "P016"  → parsear a 16
        public string Fecha { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
