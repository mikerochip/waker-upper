using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;
using Twilio.TwiML.Messaging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.LambdaJsonSerializer))]

namespace WakerUpper.Application
{
    public class Function
    {
        #region Properties
        private AmazonSimpleSystemsManagementClient SsmClient => _ssmClient.Value;
        private AmazonCloudWatchEventsClient CweClient => _cweClient.Value;

        private string SendEventRuleName { get; } = Environment.GetEnvironmentVariable("SendEventRuleName");
        private string TwilioAccountSidParameterName { get; } = Environment.GetEnvironmentVariable("TwilioAccountSidParameter");
        private string TwilioAuthTokenParameterName { get; } = Environment.GetEnvironmentVariable("TwilioAuthTokenParameter");
        private string SourcePhoneNumberParameterName { get; } = Environment.GetEnvironmentVariable("SourcePhoneNumberParameter");
        private string TargetPhoneNumberParameterName { get; } = Environment.GetEnvironmentVariable("TargetPhoneNumberParameter");
        private string MessageParameterName { get; } = Environment.GetEnvironmentVariable("MessageParameter");
        
        private string TwilioAccountSid { get; set; }
        private string TwilioAuthToken { get; set; }
        #endregion

        #region Members
        private Lazy<AmazonSimpleSystemsManagementClient> _ssmClient =
            new Lazy<AmazonSimpleSystemsManagementClient>(
                () => new AmazonSimpleSystemsManagementClient()
            );
        private Lazy<AmazonCloudWatchEventsClient> _cweClient =
            new Lazy<AmazonCloudWatchEventsClient>(
                () => new AmazonCloudWatchEventsClient()
            );

        private Dictionary<string, string> _parameterValues = new Dictionary<string, string>();
        #endregion
        
        #region Constructor
        #endregion

        #region Handlers
        public async Task SendSmsAsync(ScheduledEvent scheduledEvent, ILambdaContext context)
        {
            await FillParameterValuesAsync(
                TwilioAccountSidParameterName,
                TwilioAuthTokenParameterName,
                SourcePhoneNumberParameterName,
                TargetPhoneNumberParameterName,
                MessageParameterName);
            InitTwilioClient();
            string sourcePhoneNumber = _parameterValues[SourcePhoneNumberParameterName];
            string targetPhoneNumber = _parameterValues[TargetPhoneNumberParameterName];
            string message = _parameterValues[MessageParameterName];

            AppLogger.LogJson(new
            {
                Event = "Request",
                RequestId = context.AwsRequestId,
                SourcePhoneNumber = sourcePhoneNumber,
                TargetPhoneNumber = targetPhoneNumber,
                Message = message,
            });

            MessageResource messageResource = MessageResource.Create(
                body: message,
                from: new Twilio.Types.PhoneNumber(sourcePhoneNumber),
                to: new Twilio.Types.PhoneNumber(targetPhoneNumber)
            );

            AppLogger.LogJson(new
            {
                Event = "Response",
                RequestId = context.AwsRequestId,
                TwilioResponse = messageResource,
            });
        }

        public async Task<APIGatewayProxyResponse> ReceiveSmsAsync(APIGatewayProxyRequest proxyRequest, ILambdaContext context)
        {
            Dictionary<string, string> payload = QueryHelpers.ParseQuery(proxyRequest.Body)
                .ToDictionary(pair => pair.Key, pair=> pair.Value.FirstOrDefault());

            AppLogger.LogJson(new
            {
                Event = "Request",
                RequestId = context.AwsRequestId,
                Headers = proxyRequest.Headers,
                Body = proxyRequest.Body,
                Message = payload["Body"],
                Payload = payload,
            });

            DisableRuleRequest request = new DisableRuleRequest
            {
                Name = SendEventRuleName,
            };
            await CweClient.DisableRuleAsync(request);

            MessagingResponse response = new MessagingResponse();
            Message message = new Message(body: "Waker Upper stopped!");
            response.Append(message);
            string responseBody = response.ToString();

            AppLogger.LogJson(new
            {
                Event = "Response",
                RequestId = context.AwsRequestId,
                Body = responseBody,
            });

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)System.Net.HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/xml" },
                },
                Body = responseBody,
            };
        }
        #endregion

        #region Helpers
        private async Task FillParameterValuesAsync(params string[] parameterNames)
        {
            GetParametersRequest request = new GetParametersRequest
            {
                Names = parameterNames.ToList(),
                WithDecryption = true,
            };
            GetParametersResponse response = await SsmClient.GetParametersAsync(request);

            foreach (string parameterName in parameterNames)
            {
                Parameter parameter = response.Parameters.Find(p => p.Name == parameterName);
                _parameterValues[parameterName] = parameter.Value;
            }
        }

        private void InitTwilioClient()
        {
            string accountSid = _parameterValues[TwilioAccountSidParameterName];
            string authToken = _parameterValues[TwilioAuthTokenParameterName];
            
            if (TwilioAccountSid == accountSid && TwilioAuthToken == authToken)
                return;
            
            TwilioClient.Init(accountSid, authToken);
            TwilioAccountSid = accountSid;
            TwilioAuthToken = authToken;
        }
        #endregion
    }
}
