using System.Threading.Tasks;

namespace LC.BayesFilter
{
    public interface IPlatformServices
    {
        Task SaveDictAsync<T>(string name, T dict);
        Task<T> LoadDictAsync<T>(string name);
    }
}
