
namespace CargaDeEncuestasInternas.Interfaces
{
    public interface IDimensionLoader<TSource>
    {
        Task LoadAsync(IEnumerable<TSource> data, CancellationToken ct = default);
    }
}
