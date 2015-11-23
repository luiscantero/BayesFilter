using LC.BayesFilter;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace BayesFilter
{
    public class ConsolePlatformServices : IPlatformServices
    {
        private string _extension = ".json";

        public async Task SaveDictAsync<T>(string name, T dict)
        {
            await Task.Run(() =>
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name + _extension);
                string json = JsonConvert.SerializeObject(dict);

                File.WriteAllText(path, json);
            });
        }

        public async Task<T> LoadDictAsync<T>(string name)
        {
            return await Task<Dictionary<string, T>>.Run(() =>
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name + _extension);
                string json = File.ReadAllText(path);

                return JsonConvert.DeserializeObject<T>(json);
            });
        }
    }
}
