using Pulumi;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Iam.Inputs;

namespace WakerUpper.Infra
{
    internal static class IamUtil
    {
        public static Role CreateRole(string name, string principal, params string[] managedPolicyArns)
        {
            Output<GetPolicyDocumentResult> policyDocument = Output.Create(GetPolicyDocument.InvokeAsync(new GetPolicyDocumentArgs
            {
                Statements =
                {
                    new GetPolicyDocumentStatementArgs
                    {
                        Actions = { "sts:AssumeRole" },
                        Principals =
                        {
                            new GetPolicyDocumentStatementPrincipalArgs
                            {
                                Type = "Service",
                                Identifiers = { principal },
                            }
                        }
                    }
                }
            }));
            
            Role role = new Role(name, new RoleArgs
            {
                AssumeRolePolicy = policyDocument.Apply(p => p.Json),
                Path = "/",
            });

            foreach (string policyArn in managedPolicyArns)
            {
                RolePolicyAttachment attachment = new RolePolicyAttachment($"{name}Attachment", new RolePolicyAttachmentArgs
                {
                    Role = role.Name,
                    PolicyArn = policyArn,
                });
            }
            return role;
        }
    }
}
