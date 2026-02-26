using System.Configuration;
using CargaDeEncuestasInternas.Service;

namespace CargaDeOpiniones
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("   PIPELINE ETL: ANÁLISIS DE OPINIONES");
            Console.WriteLine("==============================================\n");

            try
            {
                // 1. Obtención de la cadena de conexión desde App.config
                string connectionString = ConfigurationManager.ConnectionStrings["connDbOpiniones"].ConnectionString;

                // 2. Instanciar el servicio de carga
                var etlService = new CargaDataEncuestaService(connectionString);

                // 3. Ejecución del flujo completo (Catálogos -> Clientes -> Productos -> Encuestas)
                // Este método unificado asegura que existan las categorías antes que los productos
                // y los clientes antes que las opiniones.
                etlService.EjecutarPipelineCompleto();

                Console.WriteLine("\n==============================================");
                Console.WriteLine("      PROCESO FINALIZADO CON ÉXITO");
                Console.WriteLine("==============================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[ERROR CRÍTICO EN EL SISTEMA]");
                Console.WriteLine($"Mensaje: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Detalle: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPresiona cualquier tecla para cerrar la consola...");
            Console.ReadKey();
        }
    }
}
    