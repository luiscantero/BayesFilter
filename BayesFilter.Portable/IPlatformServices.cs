using System.Threading.Tasks;

namespace BayesFilter.Portable
{
    public interface IPlatformServices
    {
        Task SaveDictAsync<T>(string name, T dict);
        Task<T> LoadDictAsync<T>(string name);
    }
}
