
namespace CargaDeEncuestasInternas.Models.Csv
{
        public class Encuesta
        {
            /// <summary>
            /// Código identificador proveniente del archivo origen.
            /// </summary>
            public string IdOpinion { get; set; } = string.Empty;

            /// <summary>
            /// Identificador del cliente asociado.
            /// </summary>
            public int IdCliente { get; set; }

            /// <summary>
            /// Identificador del producto evaluado.
            /// </summary>
            public int IdProducto { get; set; }

            /// <summary>
            /// Fecha en la que se realizó la encuesta.
            /// </summary>
            public DateTime Fecha { get; set; }

            /// <summary>
            /// Contenido textual de la opinión.
            /// </summary>
            public string Comentario { get; set; } = string.Empty;

            /// <summary>
            /// Clasificación de sentimiento detectada.
            /// </summary>
            public string Clasificacion { get; set; } = string.Empty;

            /// <summary>
            /// Valor numérico de la satisfacción (1-5).
            /// </summary>
            public int PuntajeSatisfaccion { get; set; }

            /// <summary>
            /// Canal u origen de donde proviene el dato.
            /// </summary>
            public string Fuente { get; set; } = string.Empty;
        }
    }

