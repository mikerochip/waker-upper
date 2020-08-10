using Pulumi;
using Pulumi.Aws.Acm;
using Pulumi.Aws.Route53;
using System.Linq;
using Pulumi.Aws.Acm.Outputs;
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
            Output<CertificateDomainValidationOption> validationOption = certificate.DomainValidationOptions.Apply(
                options => options.First());
            
            Record record = new Record("WakerUpper", new RecordArgs
            {
                Name = validationOption.Apply(o => o.ResourceRecordName!),
                Type = validationOption.Apply(o => o.ResourceRecordType!),
                ZoneId = zone.ZoneId,
                Records = new InputList<string>
                {
                    validationOption.Apply(o => o.ResourceRecordValue!),
                },
                Ttl = 60 * 15,
            });
            
            CertificateValidation validation = new CertificateValidation("WakerUpper", new CertificateValidationArgs
            {
                CertificateArn = certificate.Arn,
                ValidationRecordFqdns = new InputList<string>
                {
                    record.Fqdn
                }
            });

            Stack.DomainName = certificate.DomainName;
            Stack.DomainCertificateArn = validation.CertificateArn;
        }
        #endregion
    }
}
