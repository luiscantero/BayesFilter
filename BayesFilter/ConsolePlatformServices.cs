using LC.BayesFilter;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BayesFilter
{
    public class ConsolePlatformServices : IPlatformServices
    {
        private string _extension = ".json";

        public async Task<T> LoadDictAsync<T>(string name)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name + _extension);
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);

            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task SaveDictAsync<T>(string name, T dict)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name + _extension);
            string json = JsonSerializer.Serialize(dict);

            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }
    }
}
