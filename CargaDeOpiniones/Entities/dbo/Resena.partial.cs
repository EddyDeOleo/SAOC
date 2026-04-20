

namespace CargaDeEncuestasInternas.Entities.dbo
{
    public partial class Resena
    {
        public int idOpinionGlobal { get; set; }
        public byte Rating { get; set; }

        public string CodigoOriginal { get; set; }
        public int idCliente { get; set; }
        public int idProducto { get; set; }
        public string Fecha { get; set; }
        public string Comentario { get; set; }
    }
}
