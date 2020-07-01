using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;

namespace WakerUpper.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder(args);
            Init(builder);
            await builder.Build().RunAsync();
        }

        private static void Init(IWebHostBuilder builder)
        {
            builder.UseStartup<Startup>();
        }
    }
}
