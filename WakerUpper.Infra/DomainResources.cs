using Pulumi;
using Pulumi.Aws.Acm;
using Pulumi.Aws.Acm.Outputs;
using Pulumi.Aws.ApiGateway;
using Pulumi.Aws.ApiGateway.Inputs;
using Pulumi.Aws.Route53;
using System.Linq;

using Config = Pulumi.Config;

namespace WakerUpper.Infra
{
    internal class DomainResources
    {
        #region Constants
        private const string Subdomain = "wakerupper";
        #endregion
        
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
            Zone zone = ImportZone();
            Certificate cert = CreateCertificate(zone);
            CreateDomain(zone, cert);
        }
        #endregion
        
        #region HostedZone
        private Zone ImportZone()
        {
            Zone zone = Zone.Get("DomainZone", Config.Require("domainZoneId"));
            
            Stack.DomainName = Output.Format($"{Subdomain}.{zone.Name}");
            
            return zone;
        }
        #endregion
        
        #region Certificate
        private Certificate CreateCertificate(Zone zone)
        {
            Certificate cert = new Certificate("DomainCert", new CertificateArgs
            {
                DomainName = Stack.DomainName!,
                ValidationMethod = "DNS",
            });
            Output<CertificateDomainValidationOption> validationOption = cert.DomainValidationOptions.Apply(
                options => options.First());
            
            Record record = new Record("CertRecord", new RecordArgs
            {
                Name = validationOption.Apply(o => o.ResourceRecordName!),
                Type = validationOption.Apply(o => o.ResourceRecordType!),
                ZoneId = zone.ZoneId,
                Records = new InputList<string>
                {
                    validationOption.Apply(o => o.ResourceRecordValue!),
                },
                Ttl = 60,
            });
            
            CertificateValidation validation = new CertificateValidation("CertValidation", new CertificateValidationArgs
            {
                CertificateArn = cert.Arn,
                ValidationRecordFqdns = new InputList<string>
                {
                    record.Fqdn
                }
            });

            Stack.DomainName = cert.DomainName;
            Stack.DomainCertificateArn = validation.CertificateArn;

            return cert;
        }
        #endregion
        
        #region Domain
        private void CreateDomain(Zone zone, Certificate cert)
        {
            DomainName domainName = new DomainName("DomainName", new DomainNameArgs
            {
                Domain = Stack.DomainName!,
                RegionalCertificateArn = Stack.DomainCertificateArn!,
                EndpointConfiguration = new DomainNameEndpointConfigurationArgs
                {
                    Types = "REGIONAL",
                },
                SecurityPolicy = "TLS_1_2",
            });
            
            Record record = new Record("CnameRecord",
                new RecordArgs
                {
                    Name = domainName.Domain,
                    Type = "CNAME",
                    ZoneId = zone.ZoneId,
                    Records = new InputList<string>
                    {
                        domainName.RegionalDomainName,
                    },
                    Ttl = 15 * 60,
                });
        }
        #endregion
    }
}
