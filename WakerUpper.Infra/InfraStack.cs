using Pulumi;

namespace WakerUpper.Infra
{
    internal class InfraStack : Stack
    {
        [Output]
        public Output<string>? PipelineWebhookUrl { get; set; }
        [Output]
        public Output<string>? SourcePhoneNumberParameterName { get; set; }
        [Output]
        public Output<string>? TargetPhoneNumberParameterName { get; set; }
        [Output]
        public Output<string>? MessageParameterName { get; set; }
        [Output]
        public Output<string>? TwilioAccountSidParameterName { get; set; }
        [Output]
        public Output<string>? TwilioAuthTokenParameterName { get; set; }
        
        public InfraStack()
        {
            SsmResources ssmResources = new SsmResources(this);
            ssmResources.CreateResources();
            
            PipelineResources pipelineResources = new PipelineResources(this);
            pipelineResources.CreateResources();
        }
    }
}
