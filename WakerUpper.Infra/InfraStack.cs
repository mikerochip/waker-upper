using Pulumi;

namespace WakerUpper.Infra
{
    internal class InfraStack : Stack
    {
        [Output]
        public Output<string>? PipelineWebhookUrl { get; set; }
        
        public InfraStack()
        {
            AppPipeline pipeline = new AppPipeline(this);
            pipeline.CreateResources();
        }
    }
}
