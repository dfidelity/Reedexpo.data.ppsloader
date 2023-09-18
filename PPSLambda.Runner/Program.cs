using System;
using Newtonsoft.Json.Linq;
using PPSLambdaRunner;

namespace PPSLambda.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var main = new Main();
            dynamic input = new JObject();
            try
            {
                var fakeContext = new FakeLambdaContext();
                var result =  main.Execute(input, fakeContext).Result;
                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
