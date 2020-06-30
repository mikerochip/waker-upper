using System.Threading.Tasks;
using Pulumi;

namespace WakerUpper.Infra
{
    internal static class Program
    {
        private static Task<int> Main() => Deployment.RunAsync<PipelineStack>();
    }
}
