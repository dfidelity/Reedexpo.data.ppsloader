using System.IO;
using Newtonsoft.Json;

namespace PPSLambda.Config
{
    public static class ConfigFactory
    {
        public static Config GetConfig()
        {
            using (var r = File.OpenText(@"./config.json"))
            {
                var json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<Config>(json);
            }
        }
    }
}
