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
using CsvResena = CargaDeEncuestasInternas.Models.Csv.Resena;
using CsvComentarioSocial = CargaDeEncuestasInternas.Models.Csv.ComentarioSocial;

namespace CargaDeEncuestasInternas.Service
{
    public class CargaDataEncuestaService
    {
        private readonly string _connectionString;
        public CargaDataEncuestaService(string connectionString) => _connectionString = connectionString;

        // Método para obtener un contexto limpio en cada paso
        private OpinionesClientesContext GetContext() =>
            new OpinionesClientesContext(new DbContextOptionsBuilder<OpinionesClientesContext>()
                .UseSqlServer(_connectionString).Options);

        // ── Helper: parsear IDs con prefijo de letra ──────────────
        // "C007" → 7 | "W0001" → 1 | "T0003" → 3
        private int ParsearId(string valor)
        {
            var soloNumeros = new string(valor.Trim().Where(char.IsDigit).ToArray());
            return int.TryParse(soloNumeros, out int resultado) ? resultado : 0;
        }

        // ── Helper: obtener o crear cadena TipoCanal → Canal → Fuente ──
        private int ObtenerOCrearFuente(OpinionesClientesContext db, string nombreCanal, string tipoCanal)
        {
            nombreCanal = nombreCanal.Trim();
            tipoCanal = tipoCanal.Trim();

            // 1. Buscar o crear Cat_TiposCanal
            var tipo = db.Cat_TiposCanal.FirstOrDefault(t => t.Descripcion == tipoCanal);
            if (tipo == null)
            {
                int nuevoIdTipo = db.Cat_TiposCanal.Any()
                    ? db.Cat_TiposCanal.Max(t => t.idTipoCanal) + 1
                    : 1;
                tipo = new Cat_TiposCanal { idTipoCanal = nuevoIdTipo, Descripcion = tipoCanal };
                db.Cat_TiposCanal.Add(tipo);
                db.SaveChanges();
                Console.WriteLine($"  + TipoCanal '{tipoCanal}' creado (ID={nuevoIdTipo}).");
            }

            // 2. Buscar o crear Cat_Canal
            var canal = db.Cat_Canal.FirstOrDefault(c => c.NombreCanal == nombreCanal);
            if (canal == null)
            {
                int nuevoIdCanal = db.Cat_Canal.Any()
                    ? db.Cat_Canal.Max(c => c.idCanal) + 1
                    : 1;
                canal = new Cat_Canal { idCanal = nuevoIdCanal, NombreCanal = nombreCanal, idTipoCanal = tipo.idTipoCanal };
                db.Cat_Canal.Add(canal);
                db.SaveChanges();
                Console.WriteLine($"  + Canal '{nombreCanal}' creado (ID={nuevoIdCanal}).");
            }

            // 3. Buscar o crear Fuentes
            var fuente = db.Fuentes.FirstOrDefault(f => f.idCanal == canal.idCanal);
            if (fuente == null)
            {
                int nuevoIdFuente = db.Fuentes.Any()
                    ? db.Fuentes.Max(f => f.idFuente) + 1
                    : 1;
                fuente = new Fuentes { idFuente = nuevoIdFuente, idCanal = canal.idCanal, FechaCarga = DateOnly.FromDateTime(DateTime.Now) };
                db.Fuentes.Add(fuente);
                db.SaveChanges();
                Console.WriteLine($"  + Fuente para '{nombreCanal}' creada (ID={nuevoIdFuente}).");
            }

            return fuente.idFuente;
        }

        // ════════════════════════════════════════════════════════════
        // PUNTO DE ENTRADA
        // ════════════════════════════════════════════════════════════
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

            // ── FASE C: Cargar Encuestas ──────────────────────────
            ProcesarEncuestas();

            // ── FASE D: Reseñas Web ───────────────────────────────
            ProcesarResenas();

            // ── FASE E: Comentarios Sociales ──────────────────────
            ProcesarComentariosSociales();
        }

        // ════════════════════════════════════════════════════════════
        // FASE B — Maestros
        // ════════════════════════════════════════════════════════════
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

        // ════════════════════════════════════════════════════════════
        // FASE C — Encuestas
        // ════════════════════════════════════════════════════════════
        private void ProcesarEncuestas()
        {
            Console.WriteLine("-> Procesando Encuestas...");

            Dictionary<string, int> mapaClasif;
            using (var db = GetContext())
            {
                mapaClasif = db.Clasificacion
                    .AsNoTracking()
                    .ToDictionary(c => c.Etiqueta.ToLower(), c => c.idClasificacion);
            }

            int nextIdOpinion;
            using (var db = GetContext())
            {
                nextIdOpinion = db.Opiniones.Any()
                    ? db.Opiniones.Max(o => o.idOpinionGlobal) + 1
                    : 1;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower().Replace("ó", "o").Trim(),
                HeaderValidated = null
            };

            using var reader = new StreamReader(ConfigurationManager.AppSettings["PathFileEncuestas"]);
            using var csv = new CsvReader(reader, config);
            var registros = csv.GetRecords<CsvEncuesta>().ToList();

            Console.WriteLine($"-> {registros.Count} encuestas leídas del CSV.");

            HashSet<int> clientesValidos;
            HashSet<int> productosValidos;
            using (var db = GetContext())
            {
                clientesValidos = db.Cliente.Select(c => c.idCliente).ToHashSet();
                productosValidos = db.Producto.Select(p => p.idProducto).ToHashSet();
            }

            var registrosValidos = new List<CsvEncuesta>();
            var registrosRechazados = 0;

            foreach (var fila in registros)
            {
                if (!clientesValidos.Contains(fila.IdCliente))
                {
                    Console.WriteLine($"[RECHAZADO] Encuesta {fila.IdOpinion}: idCliente={fila.IdCliente} no existe en DB.");
                    registrosRechazados++;
                    continue;
                }
                if (!productosValidos.Contains(fila.IdProducto))
                {
                    Console.WriteLine($"[RECHAZADO] Encuesta {fila.IdOpinion}: idProducto={fila.IdProducto} no existe en DB.");
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

            Console.WriteLine($"-> Iniciando carga masiva de {registrosValidos.Count} encuestas...");

            foreach (var fila in registrosValidos)
            {
                using (var db = GetContext())
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        int idClasif = mapaClasif[fila.Clasificacion.Trim().ToLower()];

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

                        db.Encuesta.Add(new Encuesta
                        {
                            idOpinionGlobal = nextIdOpinion,
                            PuntajeSatisfaccion = (byte)fila.PuntajeSatisfaccion,
                            idClasificacion = idClasif
                        });
                        db.SaveChanges();

                        tx.Commit();
                        Console.WriteLine($"[OK] Encuesta {fila.IdOpinion} importada (idOpinionGlobal={nextIdOpinion}).");
                        nextIdOpinion++;
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

        // ════════════════════════════════════════════════════════════
        // FASE D — Reseñas Web
        // ════════════════════════════════════════════════════════════
        private void ProcesarResenas()
        {
            Console.WriteLine("-> Procesando Reseñas Web...");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower().Trim(),
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var reader = new StreamReader(ConfigurationManager.AppSettings["PathFileResenas"]);
            using var csv = new CsvReader(reader, config);
            var registros = csv.GetRecords<CsvResena>().ToList();

            Console.WriteLine($"-> {registros.Count} reseñas leídas del CSV.");

            HashSet<int> clientesValidos;
            HashSet<int> productosValidos;
            using (var db = GetContext())
            {
                clientesValidos = db.Cliente.Select(c => c.idCliente).ToHashSet();
                productosValidos = db.Producto.Select(p => p.idProducto).ToHashSet();
            }

            int nextIdOpinion;
            using (var db = GetContext())
            {
                nextIdOpinion = db.Opiniones.Any()
                    ? db.Opiniones.Max(o => o.idOpinionGlobal) + 1
                    : 1;
            }

            // Obtener o crear fuente "Web Oficial" una sola vez
            int idFuenteWeb;
            using (var db = GetContext())
            {
                idFuenteWeb = ObtenerOCrearFuente(db, "Web Oficial", "Web");
            }

            var registrosValidos = new List<CsvResena>();
            var registrosRechazados = 0;

            foreach (var fila in registros)
            {
                if (string.IsNullOrWhiteSpace(fila.IdCliente))
                {
                    Console.WriteLine($"[RECHAZADO] Reseña {fila.IdReview}: IdCliente vacío.");
                    registrosRechazados++;
                    continue;
                }

                int idCliente = ParsearId(fila.IdCliente);
                int idProducto = ParsearId(fila.IdProducto);

                if (!clientesValidos.Contains(idCliente))
                {
                    Console.WriteLine($"[RECHAZADO] Reseña {fila.IdReview}: idCliente={idCliente} no existe en DB.");
                    registrosRechazados++;
                    continue;
                }
                if (!productosValidos.Contains(idProducto))
                {
                    Console.WriteLine($"[RECHAZADO] Reseña {fila.IdReview}: idProducto={idProducto} no existe en DB.");
                    registrosRechazados++;
                    continue;
                }

                registrosValidos.Add(fila);
            }

            Console.WriteLine($"-> {registrosValidos.Count} válidas / {registrosRechazados} rechazadas por integridad referencial.");

            if (!registrosValidos.Any())
            {
                Console.WriteLine("[AVISO] No hay reseñas válidas para procesar.");
                return;
            }

            Console.WriteLine($"-> Iniciando carga masiva de {registrosValidos.Count} reseñas...");

            foreach (var fila in registrosValidos)
            {
                using (var db = GetContext())
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        int idCliente = ParsearId(fila.IdCliente);
                        int idProducto = ParsearId(fila.IdProducto);

                        var op = new Opiniones
                        {
                            idOpinionGlobal = nextIdOpinion,
                            idCliente = idCliente,
                            idProducto = idProducto,
                            Fecha = DateOnly.ParseExact(fila.Fecha.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                            Comentario = fila.Comentario.Trim(),
                            CodigoOriginal = fila.IdReview.Trim(),
                            idFuente = idFuenteWeb
                        };
                        db.Opiniones.Add(op);
                        db.SaveChanges();

                        db.Resena.Add(new Resena
                        {
                            idOpinionGlobal = nextIdOpinion,
                            Rating = (byte)fila.Rating
                        });
                        db.SaveChanges();

                        tx.Commit();
                        Console.WriteLine($"[OK] Reseña {fila.IdReview} importada (idOpinionGlobal={nextIdOpinion}).");
                        nextIdOpinion++;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        var detalle = ex.InnerException?.Message ?? ex.Message;
                        Console.WriteLine($"[ERR] Reseña {fila.IdReview}: {detalle}");
                    }
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        // FASE E — Comentarios Sociales
        // ════════════════════════════════════════════════════════════
        private void ProcesarComentariosSociales()
        {
            Console.WriteLine("-> Procesando Comentarios Sociales...");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower().Trim(),
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var reader = new StreamReader(ConfigurationManager.AppSettings["PathFileComentariosSociales"]);
            using var csv = new CsvReader(reader, config);
            var registros = csv.GetRecords<CsvComentarioSocial>().ToList();

            Console.WriteLine($"-> {registros.Count} comentarios sociales leídos del CSV.");

            HashSet<int> clientesValidos;
            HashSet<int> productosValidos;
            using (var db = GetContext())
            {
                clientesValidos = db.Cliente.Select(c => c.idCliente).ToHashSet();
                productosValidos = db.Producto.Select(p => p.idProducto).ToHashSet();
            }

            int nextIdOpinion;
            using (var db = GetContext())
            {
                nextIdOpinion = db.Opiniones.Any()
                    ? db.Opiniones.Max(o => o.idOpinionGlobal) + 1
                    : 1;
            }

            // Caché de fuentes por canal para no recrearlas en cada iteración
            var fuenteCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var registrosValidos = new List<CsvComentarioSocial>();
            var registrosRechazados = 0;

            foreach (var fila in registros)
            {
                if (string.IsNullOrWhiteSpace(fila.IdCliente))
                {
                    Console.WriteLine($"[RECHAZADO] Comentario {fila.IdComment}: IdCliente vacío.");
                    registrosRechazados++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(fila.Fuente))
                {
                    Console.WriteLine($"[RECHAZADO] Comentario {fila.IdComment}: Fuente vacía.");
                    registrosRechazados++;
                    continue;
                }

                int idCliente = ParsearId(fila.IdCliente);
                int idProducto = ParsearId(fila.IdProducto);

                if (!clientesValidos.Contains(idCliente))
                {
                    Console.WriteLine($"[RECHAZADO] Comentario {fila.IdComment}: idCliente={idCliente} no existe en DB.");
                    registrosRechazados++;
                    continue;
                }
                if (!productosValidos.Contains(idProducto))
                {
                    Console.WriteLine($"[RECHAZADO] Comentario {fila.IdComment}: idProducto={idProducto} no existe en DB.");
                    registrosRechazados++;
                    continue;
                }

                registrosValidos.Add(fila);
            }

            Console.WriteLine($"-> {registrosValidos.Count} válidos / {registrosRechazados} rechazados por integridad referencial.");

            if (!registrosValidos.Any())
            {
                Console.WriteLine("[AVISO] No hay comentarios sociales válidos para procesar.");
                return;
            }

            Console.WriteLine($"-> Iniciando carga masiva de {registrosValidos.Count} comentarios sociales...");

            foreach (var fila in registrosValidos)
            {
                using (var db = GetContext())
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        int idCliente = ParsearId(fila.IdCliente);
                        int idProducto = ParsearId(fila.IdProducto);

                        // Obtener o crear fuente con caché
                        if (!fuenteCache.ContainsKey(fila.Fuente.Trim()))
                        {
                            int idFuente = ObtenerOCrearFuente(db, fila.Fuente.Trim(), "Red Social");
                            fuenteCache[fila.Fuente.Trim()] = idFuente;
                        }
                        int idFuenteSocial = fuenteCache[fila.Fuente.Trim()];

                        var op = new Opiniones
                        {
                            idOpinionGlobal = nextIdOpinion,
                            idCliente = idCliente,
                            idProducto = idProducto,
                            Fecha = DateOnly.ParseExact(fila.Fecha.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                            Comentario = fila.Comentario.Trim(),
                            CodigoOriginal = fila.IdComment.Trim(),
                            idFuente = idFuenteSocial
                        };
                        db.Opiniones.Add(op);
                        db.SaveChanges();

                        db.ComentarioSocial.Add(new ComentarioSocial
                        {
                            idOpinionGlobal = nextIdOpinion
                        });
                        db.SaveChanges();

                        tx.Commit();
                        Console.WriteLine($"[OK] Comentario {fila.IdComment} importado (idOpinionGlobal={nextIdOpinion}).");
                        nextIdOpinion++;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        var detalle = ex.InnerException?.Message ?? ex.Message;
                        Console.WriteLine($"[ERR] Comentario {fila.IdComment}: {detalle}");
                    }
                }
            }
        }
    }
}