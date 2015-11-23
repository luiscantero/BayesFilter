using LC.BayesFilter;
using System;
using System.Threading.Tasks;

namespace BayesFilter
{
    class ConsolePlatformServices : IPlatformServices
    {
        public Task SaveDictAsync<T>(string name, T dict)
        {
            throw new NotImplementedException();
        }

        public Task<T> LoadDictAsync<T>(string name)
        {
            throw new NotImplementedException();
        }
    }
}
