using Pulumi;
using Pulumi.Aws.S3;
using Pulumi.Aws.CodePipeline;
using Pulumi.Aws.CodePipeline.Inputs;
using Pulumi.Aws.CodePipeline.Outputs;

namespace WakerUpper.Infra
{
    class MyStack : Stack
    {
        [Output]
        public Output<string> BucketName { get; set; }
        
        public MyStack()
        {
            // Create an AWS resource (S3 Bucket)
            var bucket = new Bucket("my-bucket");

            // Export the name of the bucket
            BucketName = bucket.Id;
        }
    }
}
