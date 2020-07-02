using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace WakerUpper.Application.Pages
{
    public class ErrorModel : PageModel
    {
        #region Properties
        public string RequestId { get; set; }
        #endregion
        
        #region Members
        private ILogger _logger;
        #endregion

        public ErrorModel(ILogger logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            LogErrors();
        }

        public void OnPost()
        {
            LogErrors();
        }
        
        private void LogErrors()
        {
            var lambdaContext = (ILambdaContext)HttpContext.Items[AbstractAspNetCoreFunction.LAMBDA_CONTEXT];
            RequestId = lambdaContext.AwsRequestId;
            
            var exceptionInfo = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            Exception exception = exceptionInfo?.Error;
            
            string json = JsonSerializer.Serialize(new
            {
                Event = "Request",
                RequestId = RequestId,
                Method = Request.Method,
                Path = Request.Path.Value,
                Referrer = Request.GetTypedHeaders().Referer,
                Exception = exception?.GetType().Name,
                ExceptionMessage = exception?.Message,
            });
            _logger.LogInformation(json);
        }
    }
}
