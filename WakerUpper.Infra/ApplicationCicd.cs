using Pulumi;
using Pulumi.Github;
using Pulumi.Github.Inputs;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Iam.Inputs;
using Pulumi.Aws.CloudWatch;
using Pulumi.Aws.CloudWatch.Inputs;
using Pulumi.Aws.CodeBuild;
using Pulumi.Aws.CodeBuild.Inputs;
using Pulumi.Aws.CodePipeline;
using Pulumi.Aws.CodePipeline.Inputs;
using Pulumi.Aws.S3;
using Pulumi.Aws.S3.Inputs;

using Config = Pulumi.Config;
using PipelineWebhook = Pulumi.Aws.CodePipeline.Webhook;
using PipelineWebhookArgs = Pulumi.Aws.CodePipeline.WebhookArgs;

namespace WakerUpper.Infra
{
    internal class ApplicationCicd
    {
        private InfraStack Stack { get; }
        private Config Config { get; } = new Config();
        
        public ApplicationCicd(InfraStack stack)
        {
            Stack = stack;
        }

        public void CreateResources()
        {
            Bucket bucket = CreateArtifactBucket();
            Role pipelineRole = CreatePipelineRole();
            Project buildProject = CreateBuildProject(bucket);
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

        private Project CreateBuildProject(Bucket bucket)
        {
            Role role = CreateBuildRole();
            
            Project project = new Project("WakerUpper", new ProjectArgs
            {
                ServiceRole = role.Arn,
                Artifacts = new ProjectArtifactsArgs
                {
                    Type = "CODEPIPELINE",
                },
                Source = new ProjectSourceArgs
                {
                    Type = "CODEPIPELINE",
                    Buildspec = "WakerUpper.Infra/App.buildspec.yml",
                },
                Environment = new ProjectEnvironmentArgs
                {
                    ComputeType = "BUILD_GENERAL1_SMALL",
                    Type = "LINUX_CONTAINER",
                    Image = "aws/codebuild/amazonlinux2-x86_64-standard:3.0",
                    EnvironmentVariables =
                    {
                        new ProjectEnvironmentEnvironmentVariableArgs
                        {
                            Name = "ProjectPath",
                            Value = "WakerUpper.Application",
                        },
                        new ProjectEnvironmentEnvironmentVariableArgs
                        {
                            Name = "Framework",
                            Value = "netcoreapp3.1",
                        },
                        new ProjectEnvironmentEnvironmentVariableArgs
                        {
                            Name = "Configuration",
                            Value = "Release",
                        },
                        new ProjectEnvironmentEnvironmentVariableArgs
                        {
                            Name = "ArtifactBucket",
                            Value = bucket.BucketName,
                        },
                        new ProjectEnvironmentEnvironmentVariableArgs
                        {
                            Name = "ArtifactBucketPrefix",
                            Value = "WakerUpper/Builds",
                        },
                        new ProjectEnvironmentEnvironmentVariableArgs
                        {
                            Name = "TemplateFileName",
                            Value = "template.json",
                        },
                        new ProjectEnvironmentEnvironmentVariableArgs
                        {
                            Name = "OutputTemplateFileName",
                            Value = "output-template.json",
                        },
                    }
                }
            });
            
            LogGroup logGroup = new LogGroup("WakerUpperBuilder", new LogGroupArgs
            {
                Name = $"/aws/codebuild/{project.Name}",
                RetentionInDays = 1,
            });
            
            return project;
        }

        private Role CreateBuildRole()
        {
            Role role = CreateRole(
                "WakerUpperBuild",
                "codebuild.amazonaws.com",
                "arn:aws:iam::aws:policy/AWSCodeBuildAdminAccess");
            
            // add permissions not covered by the managed policies
            Output<GetPolicyDocumentResult> policyDocument = Output.Create(GetPolicyDocument.InvokeAsync(new GetPolicyDocumentArgs
            {
                Statements =
                {
                    new GetPolicyDocumentStatementArgs
                    {
                        Resources = { "*" },
                        Actions =
                        {
                            "logs:CreateLogGroup",
                            "logs:CreateLogStream",
                            "logs:PutLogEvents",
                            "s3:GetObject",
                            "s3:GetObjectVersion",
                            "s3:PutObject",
                        },
                    }
                }
            }));
            RolePolicy policy = new RolePolicy("WakerUpperBuilder", new RolePolicyArgs
            {
                Role = role.Id,
                Policy = policyDocument.Apply(p => p.Json),
            });

            return role;
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
            string branch = Config.Require("branch");
            
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
                    MatchEquals = $"refs/heads/{branch}",
                },
                TargetAction = "Source",
                TargetPipeline = pipeline.Name,
            });
            
            Stack.PipelineWebhookUrl = webhook.Url;
            
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
