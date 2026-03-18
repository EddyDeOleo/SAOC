namespace CargaDeEncuestasInternas.Entities.API

{

    public record ComentarioSocialDto
    {
        public string IdComentario { get; set; } = string.Empty;
        public string IdCliente { get; set; } = string.Empty;
        public string IdProducto { get; set; } = string.Empty;
        public string RedSocial { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
    }
}
