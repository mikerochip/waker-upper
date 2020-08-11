using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;

namespace WakerUpper.Asp
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

            AppLogger.LogJson(new
            {
                Event = "Request",
                RequestId = lambdaContext.AwsRequestId,
                RequestPath = lambdaRequest.Path,
                RequestParameters = lambdaRequest.PathParameters,
                Method = lambdaRequest.HttpMethod,
            });
        }

        protected override void PostMarshallResponseFeature(
            IHttpResponseFeature aspNetCoreResponseFeature,
            APIGatewayProxyResponse lambdaResponse,
            ILambdaContext lambdaContext)
        {
            base.PostMarshallResponseFeature(aspNetCoreResponseFeature, lambdaResponse, lambdaContext);

            AppLogger.LogJson(new
            {
                Event = "Response",
                RequestId = lambdaContext.AwsRequestId,
                StatusCode = lambdaResponse.StatusCode,
            });
        }
    }
}
