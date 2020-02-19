using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace WakerUpper.Application
{
    public class LambdaAspEntryPoint : APIGatewayProxyFunction
    {
        protected override void Init(IWebHostBuilder builder)
        {
            builder.UseStartup<Startup>();
        }

        protected override void PostMarshallRequestFeature(
            IHttpRequestFeature aspNetCoreRequestFeature,
            APIGatewayProxyRequest lambdaRequest,
            ILambdaContext lambdaContext)
        {
            base.PostMarshallRequestFeature(aspNetCoreRequestFeature, lambdaRequest, lambdaContext);

            JObject errorObj = JObject.FromObject(new
            {
                Event = "Request",
                RequestId = lambdaContext.AwsRequestId,
            });
            Console.WriteLine(errorObj.ToString(Formatting.None));
        }

        protected override void PostMarshallResponseFeature(
            IHttpResponseFeature aspNetCoreResponseFeature,
            APIGatewayProxyResponse lambdaResponse,
            ILambdaContext lambdaContext)
        {
            base.PostMarshallResponseFeature(aspNetCoreResponseFeature, lambdaResponse, lambdaContext);

            JObject errorObj = JObject.FromObject(new
            {
                Event = "Response",
                RequestId = lambdaContext.AwsRequestId,
            });
            Console.WriteLine(errorObj.ToString(Formatting.None));
        }
    }
}
