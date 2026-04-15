

namespace CargaDeEncuestasInternas.Interfaces
{
    public interface IFactLoader<TSource>
    {
        Task LoadFactAsync(IEnumerable<TSource> data, CancellationToken ct = default);
    }
}
