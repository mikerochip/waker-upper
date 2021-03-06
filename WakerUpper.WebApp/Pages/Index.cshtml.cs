﻿using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace WakerUpper.WebApp.Pages
{
    public class IndexModel : PageModel
    {
        #region Model Properties
        [BindProperty]
        [Phone]
        public string PhoneNumber { get; set; }
        
        public List<SelectListItem> PhoneNumberOptions { get; } = new List<SelectListItem>();
        
        [BindProperty]
        [MinLength(1), MaxLength(1000)]
        public string Message { get; set; }
        
        public bool IsEnabled { get; set; }
        #endregion

        #region Properties
        private string SendEventRuleName { get; } = Environment.GetEnvironmentVariable("SendEventRuleName");
        private string TargetPhoneNumberParameterName { get; } = Environment.GetEnvironmentVariable("TargetPhoneNumberParameter");
        private string MessageParameterName { get; } = Environment.GetEnvironmentVariable("MessageParameter");
        private string PhoneNumberOptionsParameterPath { get; } = Environment.GetEnvironmentVariable("PhoneNumberOptionsParameterPath");
        #endregion

        #region Members
        private ILogger _logger;
        
        private IAmazonCloudWatchEvents _cloudWatchEvents;
        private IAmazonSimpleSystemsManagement _ssm;

        private Dictionary<string, string> _parameterValues = new Dictionary<string, string>();
        #endregion

        #region Body
        public IndexModel(
            ILogger logger,
            IAmazonCloudWatchEvents cloudWatchEvents,
            IAmazonSimpleSystemsManagement ssm)
        {
            _logger = logger;
            
            _cloudWatchEvents = cloudWatchEvents;
            _ssm = ssm;
        }

        public async Task OnGetAsync()
        {
            LogPageRequest(nameof(OnGetAsync));

            DescribeRuleRequest request = new DescribeRuleRequest
            {
                Name = SendEventRuleName,
            };
            DescribeRuleResponse response = await _cloudWatchEvents.DescribeRuleAsync(request);
            IsEnabled = (response.State == RuleState.ENABLED);

            await FillParameterValuesAsync(
                TargetPhoneNumberParameterName,
                MessageParameterName);
            PhoneNumber = _parameterValues[TargetPhoneNumberParameterName];
            Message = _parameterValues[MessageParameterName];

            await FillPhoneNumberOptionsAsync();
            
            LogPageResponse(nameof(OnGetAsync));
        }

        public async Task<IActionResult> OnPostEnableAsync(bool enable)
        {
            LogPageRequest(nameof(OnPostEnableAsync));

            List<Task> tasks = new List<Task>();
            tasks.Add(UpdatePhoneNumberAsync());
            tasks.Add(UpdateMessageAsync());
            await Task.WhenAll(tasks);
            
            if (enable)
            {
                EnableRuleRequest request = new EnableRuleRequest
                {
                    Name = SendEventRuleName,
                };
                await _cloudWatchEvents.EnableRuleAsync(request);
            }
            else
            {
                DisableRuleRequest request = new DisableRuleRequest
                {
                    Name = SendEventRuleName,
                };
                await _cloudWatchEvents.DisableRuleAsync(request);
            }
            
            LogPageResponse(nameof(OnPostEnableAsync));
            
            if (ModelState.IsValid)
                return RedirectToPage();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveParametersAsync()
        {
            LogPageRequest(nameof(OnPostSaveParametersAsync));
            
            if (ModelState.IsValid)
            {
                List<Task> tasks = new List<Task>();
                tasks.Add(UpdatePhoneNumberAsync());
                tasks.Add(UpdateMessageAsync());
                await Task.WhenAll(tasks);
            }
            
            LogPageResponse(nameof(OnPostSaveParametersAsync));
            
            if (ModelState.IsValid)
                return RedirectToPage();
            return Page();
        }
        #endregion

        #region Helpers
        private void LogPageRequest(string functionName)
        {
            LogPage("Request", functionName);
        }
        
        private void LogPageResponse(string functionName)
        {
            LogPage("Response", functionName);
        }
        
        private void LogPage(string eventName, string functionName)
        {
            var lambdaContext = (ILambdaContext)HttpContext.Items[AbstractAspNetCoreFunction.LAMBDA_CONTEXT];

            string json = JsonSerializer.Serialize(new
            {
                Event = eventName,
                LambdaRequestId = lambdaContext.AwsRequestId,
                Function = functionName,
                Method = Request.Method,
                Path = Request.Path.Value,
                EventRuleName = SendEventRuleName,
                TargetPhoneNumberParameterName = TargetPhoneNumberParameterName,
                TargetPhoneNumber = PhoneNumber,
                MessageParameterName = MessageParameterName,
                Message = Message,
                IsEnabled = IsEnabled,
                ModelState = ModelState,
            });
            _logger.LogInformation(json);
        }

        private async Task FillParameterValuesAsync(params string[] parameterNames)
        {
            GetParametersRequest request = new GetParametersRequest
            {
                Names = parameterNames.ToList(),
                WithDecryption = true,
            };
            GetParametersResponse response = await _ssm.GetParametersAsync(request);

            foreach (string parameterName in parameterNames)
            {
                Parameter parameter = response.Parameters.Find(p => p.Name == parameterName);
                _parameterValues[parameterName] = parameter.Value;
            }
        }

        private async Task FillPhoneNumberOptionsAsync()
        {
            GetParametersByPathRequest request = new GetParametersByPathRequest
            {
                Path = PhoneNumberOptionsParameterPath,
                WithDecryption = true,
            };
            GetParametersByPathResponse response = await _ssm.GetParametersByPathAsync(request);

            PhoneNumberOptions.Clear();
            
            IEnumerable<Parameter> sortedParameters = (from p in response.Parameters orderby p.Name select p);
            foreach (Parameter parameter in sortedParameters)
            {
                PhoneNumberOptions.Add(new SelectListItem
                {
                    Text = parameter.Name.Substring(parameter.Name.LastIndexOf('/') + 1),
                    Value = parameter.Value,
                });
            }
        }

        private async Task UpdatePhoneNumberAsync()
        {
            PutParameterRequest request = new PutParameterRequest
            {
                Name = TargetPhoneNumberParameterName,
                Overwrite = true,
                Value = PhoneNumber,
            };
            await _ssm.PutParameterAsync(request);
        }

        private async Task UpdateMessageAsync()
        {
            PutParameterRequest request = new PutParameterRequest
            {
                Name = MessageParameterName,
                Overwrite = true,
                Value = Message,
            };
            await _ssm.PutParameterAsync(request);
        }
        #endregion
    }
}
