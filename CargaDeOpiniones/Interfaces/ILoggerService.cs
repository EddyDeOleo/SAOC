
namespace CargaDeEncuestasInternas.Interfaces
{
    public interface ILoggerService
    {
        void LogInfo(string mensaje);
        void LogWarning(string mensaje);
        void LogError(string mensaje, Exception? ex = null);
    }

}
