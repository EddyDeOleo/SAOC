
namespace CargaDeEncuestasInternas.Interfaces
{
    public interface IExtractor<TResult> where TResult : class
    {
        string NombreFuente { get; }
        Task<IEnumerable<TResult>> ExtractAsync(
            CancellationToken cancellationToken = default);
    }
}
