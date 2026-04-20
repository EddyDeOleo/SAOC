using CargaDeEncuestasInternas.Interfaces;
using CargaDeEncuestasInternas.Models.DTOs;
using EFCore.BulkExtensions;
using EncuestasInternas.Entities;
using EncuestasInternas.Entities.FactSchema;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CargaDeEncuestasInternas.Service.Loaders.Fact
{
    public sealed class OpinionesFactLoader : IFactLoader<OpinionExtraidaDto>
    {
        private readonly DWHOpinionesClientesContext_ETL _db;

        private static readonly Dictionary<string, int> _canalPorFuente = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CSV"] = 1,
            ["DATABASE"] = 2,
            ["API"] = 3
        };

        private static readonly Dictionary<string, int> _tipoOpinionPorFuente = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CSV"] = 1,
            ["DATABASE"] = 2,
            ["API"] = 3
        };

        private static readonly Dictionary<string, int> _clasificacionPorEtiqueta = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Positiva"] = 1,
            ["Negativa"] = 2,
            ["Neutra"] = 3
        };

        private const int IdClasificacionNeutraPorDefecto = 3;

        private static readonly string[] _formatosFecha =
            ["yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd"];

        public OpinionesFactLoader(DWHOpinionesClientesContext_ETL db) => _db = db;

        public async Task LoadFactAsync(IEnumerable<OpinionExtraidaDto> data, CancellationToken ct = default)
        {
 
            var lookupTiempo = await _db.Tiempo
                .Select(t => new { t.Fecha, t.idTiempo })
                .ToDictionaryAsync(t => t.Fecha, t => t.idTiempo, ct);

            var entidades = data
                .Where(d => EsRegistroValido(d, lookupTiempo))  
                .Select(d => MapearAHecho(d, lookupTiempo))
                .ToArray();

            await _db.BulkInsertAsync(entidades, cancellationToken: ct);
        }

        private static bool EsRegistroValido(
            OpinionExtraidaDto d,
            Dictionary<DateOnly, int> lookupTiempo)
        {
            if (!TryParseFecha(d.Fecha, out var fecha)) return false;
            if (!lookupTiempo.ContainsKey(fecha)) return false;

            if (!int.TryParse(d.IdCliente, out _)) return false;
            if (!int.TryParse(d.IdProducto, out _)) return false;

            if (!_canalPorFuente.ContainsKey(d.FuenteOrigen)) return false;

            return true;
        }

        private static Opiniones MapearAHecho(
            OpinionExtraidaDto d,
            Dictionary<DateOnly, int> lookupTiempo)
        {
            TryParseFecha(d.Fecha, out var fecha);

            var idClasificacion = ResolverClasificacion(d);
            var esPositiva = (byte)(idClasificacion == 1 ? 1 : 0);
            var esNegativa = (byte)(idClasificacion == 2 ? 1 : 0);
            var esNeutra = (byte)(idClasificacion == 3 ? 1 : 0);

            return new Opiniones
            {
                idTiempo = lookupTiempo[fecha],
                idCanal = _canalPorFuente[d.FuenteOrigen],
                idProducto = int.Parse(d.IdProducto),
                idCliente = int.Parse(d.IdCliente),
                idClasificacion = idClasificacion,
                idTipoOpinion = _tipoOpinionPorFuente[d.FuenteOrigen],
                CodigoOriginal = d.CodigoOriginal,
                PuntajeSatisfaccion = d.PuntajeSatisfaccion.HasValue
                                        ? (byte?)d.PuntajeSatisfaccion.Value
                                        : null,
                Rating = d.Rating.HasValue
                                        ? (byte?)d.Rating.Value
                                        : null,
                EsPositiva = esPositiva,
                EsNegativa = esNegativa,
                EsNeutra = esNeutra,
                ConteoOpinion = 1
            };
        }


        private static int ResolverClasificacion(OpinionExtraidaDto d)
        {
            if (!string.IsNullOrWhiteSpace(d.Clasificacion)
                && _clasificacionPorEtiqueta.TryGetValue(d.Clasificacion, out var id))
                return id;

            var puntaje = d.PuntajeSatisfaccion ?? d.Rating;
            if (puntaje.HasValue)
                return puntaje.Value switch
                {
                    >= 4 => 1, 
                    <= 2 => 2,  
                    _ => 3  
                };

            return ClasificarPorTexto(d.Comentario);
        }

        private static readonly string[] _palabrasPositivas =
            ["excelente", "genial", "increíble", "recomiendo", "satisfecho",
     "perfecto", "bueno", "rápido", "eficiente", "feliz"];

        private static readonly string[] _palabrasNegativas =
            ["malo", "pésimo", "terrible", "decepcionado", "lento",
     "horrible", "insatisfecho", "problema", "error", "deficiente"];

        private static int ClasificarPorTexto(string? comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario)) return 3;

            var texto = comentario.ToLowerInvariant();

            var positivas = _palabrasPositivas.Count(p => texto.Contains(p));
            var negativas = _palabrasNegativas.Count(p => texto.Contains(p));

            if (positivas > negativas) return 1; 
            if (negativas > positivas) return 2;  
            return 3;                            
        }

        private static bool TryParseFecha(string? raw, out DateOnly fecha)
        {
            fecha = DateOnly.MinValue;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var pudo = DateTime.TryParseExact(
                raw, _formatosFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt);

            if (!pudo) pudo = DateTime.TryParse(raw, out dt);
            if (!pudo) return false;

            fecha = DateOnly.FromDateTime(dt);
            return true;
        }
    }
}