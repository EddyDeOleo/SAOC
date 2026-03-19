using System.Text;
 
const int TOTAL_REGISTROS = 17_000;
const int MAX_CLIENTES = 500;
const int MAX_PRODUCTOS = 200;
const string RUTA_SALIDA = @"C:\Users\User\Desktop\ITLA MATERIAS\ITLA C-1 2026\Electiva 1 (Big Data)\encuestas_test.csv";

var clasificaciones = new[] { "Positiva", "Negativa", "Neutra" };
var comentarios = new[]
{
    "El producto cumple su funcion, nada especial.",
    "No era lo que esperaba, muy decepcionado.",
    "El servicio fue rapido y eficiente, estoy muy satisfecho.",
    "El servicio fue lento y el soporte no resolvio mi problema.",
    "El envio fue normal, dentro del tiempo esperado.",
    "Excelente calidad, lo recomiendo ampliamente.",
    "Producto defectuoso, tuve que devolverlo.",
    "Muy buena relacion calidad-precio.",
    "Regular, esperaba algo mejor.",
    "Increible producto, lo comprare de nuevo.",
    "La atencion al cliente fue excelente.",
    "No cumple con lo anunciado, insatisfecho.",
    "Producto llegó rapido y funciona perfecto.",
    "Calidad superior, muy contento con la compra.",
    "No funciona como esperaba, decepcionado."
};

var sb = new StringBuilder();
var rng = new Random(42); // seed fija para reproducibilidad

// Cabecera — misma que tu CSV original
sb.AppendLine("IdOpinion,IdCliente,IdProducto,Fecha,Comentario,Clasificación,PuntajeSatisfacción,Fuente");

var startDate = new DateTime(2024, 1, 1);

for (int i = 1; i <= TOTAL_REGISTROS; i++)
{
    int idCliente = (i % MAX_CLIENTES) + 1;
    int idProducto = (i % MAX_PRODUCTOS) + 1;
    string clasificacion = clasificaciones[i % 3];
    int puntaje = clasificacion switch
    {
        "Positiva" => rng.Next(4, 6),   // 4-5
        "Negativa" => rng.Next(1, 3),   // 1-2
        _ => 3                  // Neutra = 3
    };
    var fecha = startDate.AddDays(i % 365).ToString("yyyy-MM-dd");
    string comentario = comentarios[i % comentarios.Length];

    // Encerrar comentario en comillas si contiene coma
    if (comentario.Contains(','))
        comentario = $"\"{comentario}\"";

    sb.AppendLine($"{i},{idCliente},{idProducto},{fecha},{comentario},{clasificacion},{puntaje},EncuestaInterna");
}

File.WriteAllText(RUTA_SALIDA, sb.ToString(), Encoding.UTF8);

Console.WriteLine($"✓ CSV generado: {RUTA_SALIDA}");
Console.WriteLine($"  Registros: {TOTAL_REGISTROS:N0}");
Console.WriteLine($"  Tamaño: {new FileInfo(RUTA_SALIDA).Length / 1024:N0} KB");