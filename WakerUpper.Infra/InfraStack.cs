using Pulumi;

namespace WakerUpper.Infra
{
    internal class InfraStack : Stack
    {
        [Output]
        public Output<string>? PipelineWebhookUrl { get; set; }
        
        public InfraStack()
        {
            AppCicd cicd = new AppCicd(this);
            cicd.CreateResources();
        }
    }
}
