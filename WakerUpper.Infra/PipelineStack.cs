using Pulumi;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Iam.Inputs;
using Pulumi.Aws.CodeBuild;
using Pulumi.Aws.CodeBuild.Inputs;
using Pulumi.Aws.CodePipeline;
using Pulumi.Aws.CodePipeline.Inputs;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;

namespace WakerUpper.Infra
{
    class PipelineStack : Stack
    {
        [Output]
        public Output<string> WebhookUrl { get; set; }
        
        public PipelineStack()
        {
            Bucket bucket = CreateArtifactBucket();
            Role pipelineRole = CreatePipelineRole();
            Project buildProject = CreateBuildProject();
            Role cloudFormationRole = CreateCloudFormationRole();
            CreatePipeline(bucket, pipelineRole, buildProject, cloudFormationRole);
        }

        private Bucket CreateArtifactBucket()
        {
            Bucket bucket = new Bucket("waker-upper-artifacts", new BucketArgs
            {
                LifecycleRules = new InputList<BucketLifecycleRuleArgs>
                {
                    new BucketLifecycleRuleArgs
                    {
                        Expiration = new BucketLifecycleRuleExpirationArgs
                        {
                            Days = 1,
                        }
                    }
                }
            });
            return bucket;
        }

        private Role CreatePipelineRole()
        {
            return CreateRole(
                "WakerUpperPipeline",
                "codepipeline.amazonaws.com",
                "arn:aws:iam::aws:policy/AdministratorAccess");
        }

        private Role CreateCloudFormationRole()
        {
            return CreateRole(
                "WakerUpperCloudFormation",
                "cloudformation.amazonaws.com",
                "arn:aws:iam::aws:policy/AdministratorAccess");
        }

        private Role CreateRole(string name, string principal, params string[] managedPolicyArns)
        {
            Output<GetPolicyDocumentResult> policy = Output.Create(GetPolicyDocument.InvokeAsync(new GetPolicyDocumentArgs
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
                AssumeRolePolicy = policy.Apply(p => p.Json),
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

        private Project CreateBuildProject()
        {
            Role role = CreateRole(
                "WakerUpperBuild",
                "codebuild.amazonaws.com",
                "arn:aws:iam::aws:policy/AWSCodeBuildAdminAccess");
            
            Project project = new Project("WakerUpper", new ProjectArgs
            {
                
            });
            return project;
        }
        
        private void CreatePipeline(Bucket bucket, Role pipelineRole, Project buildProject, Role cloudFormationRole)
        {
            Pipeline pipeline = new Pipeline("WakerUpper", new PipelineArgs
            {
                ArtifactStore = new PipelineArtifactStoreArgs
                {
                    Type = "S3",
                    Location = bucket.BucketName,
                }
            });
        }
    }
}
