using Pulumi;

namespace WakerUpper.Infra
{
    internal class InfraStack : Stack
    {
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
        [Output]
        public Output<string>? DomainName { get; set; }
        [Output]
        public Output<string>? DomainCertificateArn { get; set; }
        
        public InfraStack()
        {
            DomainResources domainResources = new DomainResources(this);
            domainResources.CreateResources();
            
            SsmResources ssmResources = new SsmResources(this);
            ssmResources.CreateResources();
            
            PipelineResources pipelineResources = new PipelineResources(this);
            pipelineResources.CreateResources();
        }
    }
}
