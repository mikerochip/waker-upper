using Pulumi;
using Pulumi.Github;
using Pulumi.Github.Inputs;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Iam.Inputs;
using Pulumi.Aws.CodeBuild;
using Pulumi.Aws.CodePipeline;
using Pulumi.Aws.CodePipeline.Inputs;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;

using Config = Pulumi.Config;
using PipelineWebhook = Pulumi.Aws.CodePipeline.Webhook;
using PipelineWebhookArgs = Pulumi.Aws.CodePipeline.WebhookArgs;

namespace WakerUpper.Infra
{
    class CicdStack : Stack
    {
        [Output]
        public Output<string> PipelineWebhookUrl { get; set; }
        
        private Config Config { get; set; }
        
        public CicdStack()
        {
            Config = new Config();
            
            Bucket bucket = CreateArtifactBucket();
            Role pipelineRole = CreatePipelineRole();
            Project buildProject = CreateBuildProject();
            Role cloudFormationRole = CreateCloudFormationRole();
            Pipeline pipeline = CreatePipeline(bucket, pipelineRole, buildProject, cloudFormationRole);
            PipelineWebhook webhook = CreatePipelineWebhook(pipeline);
            CreateRepoWebhook(webhook);
        }

        private Bucket CreateArtifactBucket()
        {
            Bucket bucket = new Bucket("waker-upper-artifacts", new BucketArgs
            {
                LifecycleRules = new BucketLifecycleRuleArgs
                {
                    Expiration = new BucketLifecycleRuleExpirationArgs
                    {
                        Days = 1,
                    },
                },
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
        
        private Pipeline CreatePipeline(Bucket bucket, Role pipelineRole, Project buildProject, Role cloudFormationRole)
        {
            Pipeline pipeline = new Pipeline("WakerUpper", new PipelineArgs
            {
                ArtifactStore = new PipelineArtifactStoreArgs
                {
                    Type = "S3",
                    Location = bucket.BucketName,
                }
            });
            return pipeline;
        }

        private PipelineWebhook CreatePipelineWebhook(Pipeline pipeline)
        {
            PipelineWebhook webhook = new PipelineWebhook("WakerUpper", new PipelineWebhookArgs
            {
                Authentication = "GITHUB_HMAC",
                AuthenticationConfiguration = new WebhookAuthenticationConfigurationArgs
                {
                    SecretToken = Config.RequireSecret("webhookSecret"),
                },
                Filters = new WebhookFilterArgs
                {
                    JsonPath = "$.ref",
                    MatchEquals = "refs/heads/master",
                },
                TargetAction = "Source",
                TargetPipeline = pipeline.Name,
            });
            
            PipelineWebhookUrl = webhook.Url;
            
            return webhook;
        }

        private void CreateRepoWebhook(PipelineWebhook pipelineWebhook)
        {
            RepositoryWebhook repoWebhook = new RepositoryWebhook($"Pulumi-CodePipeline", new RepositoryWebhookArgs
            {
                Repository = "waker-upper",
                Events = "push",
                Configuration = new RepositoryWebhookConfigurationArgs
                {
                    ContentType = "json",
                    InsecureSsl = false,
                    Secret = Config.RequireSecret("webhookSecret"),
                    Url = pipelineWebhook.Url,
                }
            });
        }
    }
}
