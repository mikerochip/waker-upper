using Pulumi;
using Pulumi.Aws.Acm;
using Pulumi.Aws.Route53;
using Config = Pulumi.Config;

namespace WakerUpper.Infra
{
    internal class DomainResources
    {
        #region Properties
        private InfraStack Stack { get; }
        private Config Config { get; } = new Config();
        #endregion
        
        #region Initialization
        public DomainResources(InfraStack stack)
        {
            Stack = stack;
        }

        public void CreateResources()
        {
            CreateCertificate();
        }
        #endregion
        
        #region Certificate
        private void CreateCertificate()
        {
            Zone zone = Zone.Get("DomainZone", Config.Require("domainZoneId"));
            
            Certificate certificate = new Certificate("WakerUpper", new CertificateArgs
            {
                DomainName = Output.Format($"wakerupper.{zone.Name}"),
                ValidationMethod = "DNS",
            });

            Stack.DomainName = certificate.DomainName;
            Stack.DomainCertificateArn = certificate.Arn;
        }
        #endregion
    }
}
