using CargaDeEncuestasInternas.Models;
using CargaDeEncuestasInternas.Models.dboSchema;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;

using System.Globalization;

using System.Configuration;
using CsvEncuesta = CargaDeEncuestasInternas.Models.Csv.Encuesta;
using CsvCliente = CargaDeEncuestasInternas.Models.Csv.Cliente;
using CsvProducto = CargaDeEncuestasInternas.Models.Csv.Producto;

namespace CargaDeEncuestasInternas.Service
{
    public class CargaDataEncuestaService
    {
        private readonly string _connectionString;
        public CargaDataEncuestaService(string connectionString) => _connectionString = connectionString;

        // Método para obtener un contexto limpio en cada paso
        private OpinionesClientesContext GetContext() =>
            new OpinionesClientesContext(new DbContextOptionsBuilder<OpinionesClientesContext>().UseSqlServer(_connectionString).Options);

        public void EjecutarPipelineCompleto()
        {
            // ── FASE 0: Infraestructura base ─────────────────────
            using (var db = GetContext())
            {
                Console.WriteLine("-> Verificando infraestructura base...");

                if (!db.Cat_TiposCanal.Any(t => t.idTipoCanal == 1))
                {
                    db.Cat_TiposCanal.Add(new Cat_TiposCanal { idTipoCanal = 1, Descripcion = "Encuesta" });
                    db.SaveChanges();
                }

                if (!db.Cat_Canal.Any(c => c.idCanal == 1))
                {
                    db.Cat_Canal.Add(new Cat_Canal { idCanal = 1, NombreCanal = "Canal Encuesta", idTipoCanal = 1 });
                    db.SaveChanges();
                }

                if (!db.Fuentes.Any(f => f.idFuente == 1))
                {
                    db.Fuentes.Add(new Fuentes { idFuente = 1, idCanal = 1, FechaCarga = DateOnly.FromDateTime(DateTime.Now) });
                    db.SaveChanges();
                }
            }

            // ── FASE A: Sincronizar Clasificaciones ───────────────
            using (var db = GetContext())
            {
                Console.WriteLine("-> Configurando Catálogo de Clasificaciones...");
                var etiquetas = new[] { "Positiva", "Negativa", "Neutra" };

                int nextId = db.Clasificacion.Any()
                    ? db.Clasificacion.Max(c => c.idClasificacion) + 1
                    : 1;

                foreach (var e in etiquetas)
                {
                    if (!db.Clasificacion.Any(c => c.Etiqueta == e))
                    {
                        db.Clasificacion.Add(new Clasificacion { idClasificacion = nextId, Etiqueta = e });
                        nextId++;
                    }
                }
                db.SaveChanges();
            }

            // ── FASE B: Cargar Clientes y Productos (Maestros) ────
            using (var db = GetContext())
            {
                Console.WriteLine("-> Cargando Maestros (Clientes y Productos)...");
                CargarMaestros(db);
            }

            // ── FASE C: Cargar Encuestas (Proceso Transaccional) ──
            ProcesarEncuestas();
        }

        private void CargarMaestros(OpinionesClientesContext db)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower().Replace("í", "i").Replace("á", "a").Trim(),
                HeaderValidated = null,
                MissingFieldFound = null
            };

            // 1. Importar Clientes
            using (var r = new StreamReader(ConfigurationManager.AppSettings["PathFileClientes"]))
            using (var csv = new CsvReader(r, config))
            {
                var clientes = csv.GetRecords<CsvCliente>().ToList();
                foreach (var c in clientes)
                    if (!db.Cliente.Any(x => x.idCliente == c.IdCliente))
                        db.Cliente.Add(new Cliente { idCliente = c.IdCliente, Nombre = c.Nombre, Email = c.Email });
            }
            db.SaveChanges();

            // 2. Importar Productos y Categorías dinámicamente
            using (var r = new StreamReader(ConfigurationManager.AppSettings["PathFileProductos"]))
            using (var csv = new CsvReader(r, config))
            {
                var prods = csv.GetRecords<CsvProducto>().ToList();

                // Caché local: nombre → id (solo IDs, sin entidades tracked)
                var categoriaCache = db.Categoria
                    .AsNoTracking()
                    .ToDictionary(c => c.Nombre.ToLower(), c => c.idCategoria);

                foreach (var p in prods)
                {
                    if (!db.Producto.Any(x => x.idProducto == p.IdProducto))
                    {
                        var keyLower = p.Categoria.Trim().ToLower();

                        if (!categoriaCache.ContainsKey(keyLower))
                        {
                            int nuevoId = db.Categoria.Any()
                                ? db.Categoria.Max(c => c.idCategoria) + 1
                                : 1;

                            var nuevaCat = new Categoria { idCategoria = nuevoId, Nombre = p.Categoria.Trim() };
                            db.Categoria.Add(nuevaCat);
                            db.SaveChanges();
                            categoriaCache[keyLower] = nuevoId;
                            db.ChangeTracker.Clear();
                        }

                        db.Producto.Add(new Producto
                        {
                            idProducto = p.IdProducto,
                            Nombre = p.Nombre.Trim(),
                            idCategoria = categoriaCache[keyLower]
                        });
                    }
                }
            }
            db.SaveChanges();
        }

        private void ProcesarEncuestas()
        {
            // ── Mapeo de clasificaciones ──────────────────────────
            Dictionary<string, int> mapaClasif;
            using (var db = GetContext())
            {
                mapaClasif = db.Clasificacion
                    .AsNoTracking()
                    .ToDictionary(c => c.Etiqueta.ToLower(), c => c.idClasificacion);
            }

            // ── Calcular próximo ID de Opiniones ──────────────────
            int nextIdOpinion;
            using (var db = GetContext())
            {
                nextIdOpinion = db.Opiniones.Any()
                    ? db.Opiniones.Max(o => o.idOpinionGlobal) + 1
                    : 1;
            }

            // ── Leer CSV ──────────────────────────────────────────
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower().Replace("ó", "o").Trim(),
                HeaderValidated = null
            };

            using var reader = new StreamReader(ConfigurationManager.AppSettings["PathFileEncuestas"]);
            using var csv = new CsvReader(reader, config);
            var registros = csv.GetRecords<CsvEncuesta>().ToList();

            Console.WriteLine($"-> {registros.Count} encuestas leídas del CSV.");

            // ── Cargar IDs válidos desde la DB ────────────────────
            HashSet<int> clientesValidos;
            HashSet<int> productosValidos;
            using (var db = GetContext())
            {
                clientesValidos = db.Cliente.Select(c => c.idCliente).ToHashSet();
                productosValidos = db.Producto.Select(p => p.idProducto).ToHashSet();
            }

            // ── Validar registros ANTES de procesar ───────────────
            var registrosValidos = new List<CsvEncuesta>();
            var registrosRechazados = 0;

            foreach (var fila in registros)
            {
                if (!clientesValidos.Contains(fila.IdCliente))
                {
                    Console.WriteLine($"[RECHAZADO] Encuesta {fila.IdOpinion}: idCliente={fila.IdCliente} no existe en la DB.");
                    registrosRechazados++;
                    continue;
                }
                if (!productosValidos.Contains(fila.IdProducto))
                {
                    Console.WriteLine($"[RECHAZADO] Encuesta {fila.IdOpinion}: idProducto={fila.IdProducto} no existe en la DB.");
                    registrosRechazados++;
                    continue;
                }
                if (!mapaClasif.ContainsKey(fila.Clasificacion.Trim().ToLower()))
                {
                    Console.WriteLine($"[RECHAZADO] Encuesta {fila.IdOpinion}: clasificación '{fila.Clasificacion}' no reconocida.");
                    registrosRechazados++;
                    continue;
                }
                registrosValidos.Add(fila);
            }

            Console.WriteLine($"-> {registrosValidos.Count} válidas / {registrosRechazados} rechazadas por integridad referencial.");

            if (!registrosValidos.Any())
            {
                Console.WriteLine("[AVISO] No hay encuestas válidas para procesar. Revisa los CSVs fuente.");
                return;
            }

            // ── Procesar solo los registros válidos ───────────────
            Console.WriteLine($"-> Iniciando carga masiva de {registrosValidos.Count} encuestas...");

            foreach (var fila in registrosValidos)
            {
                using (var db = GetContext())
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        int idClasif = mapaClasif[fila.Clasificacion.Trim().ToLower()];

                        // 1. Inserción en tabla PADRE (Opiniones)
                        var op = new Opiniones
                        {
                            idOpinionGlobal = nextIdOpinion,
                            idCliente = fila.IdCliente,
                            idProducto = fila.IdProducto,
                            Fecha = DateOnly.FromDateTime(fila.Fecha),
                            Comentario = fila.Comentario.Trim(),
                            CodigoOriginal = fila.IdOpinion,
                            idFuente = 1
                        };
                        db.Opiniones.Add(op);
                        db.SaveChanges();

                        // 2. Inserción en tabla HIJA (Encuesta) — comparten PK
                        db.Encuesta.Add(new Encuesta
                        {
                            idOpinionGlobal = nextIdOpinion,
                            PuntajeSatisfaccion = (byte)fila.PuntajeSatisfaccion,
                            idClasificacion = idClasif
                        });
                        db.SaveChanges();

                        tx.Commit();
                        Console.WriteLine($"[OK] Encuesta {fila.IdOpinion} importada (idOpinionGlobal={nextIdOpinion}).");
                        nextIdOpinion++; // Solo incrementa si el commit fue exitoso
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        var detalle = ex.InnerException?.Message ?? ex.Message;
                        Console.WriteLine($"[ERR] {fila.IdOpinion}: {detalle}");
                    }
                }
            }
        }
    }
}