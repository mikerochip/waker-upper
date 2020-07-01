using Pulumi;

namespace WakerUpper.Infra
{
    internal class InfraStack : Stack
    {
        [Output]
        public Output<string>? PipelineWebhookUrl { get; set; }
        
        public InfraStack()
        {
            ApplicationCicd cicd = new ApplicationCicd(this);
            cicd.CreateResources();
        }
    }
}
