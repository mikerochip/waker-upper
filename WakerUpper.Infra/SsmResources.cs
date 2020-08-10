using Pulumi.Aws.Ssm;

using Config = Pulumi.Config;

namespace WakerUpper.Infra
{
    internal class SsmResources
    {
        #region Constants
        private const string FakePhoneNumber = "+15555550100";
        private const string PhoneNumberRegex = "^\\+[1-9]\\d{1,14}$";
        #endregion
        
        #region Properties
        private InfraStack Stack { get; }
        private Config Config { get; } = new Config();
        #endregion
        
        #region Initialization
        public SsmResources(InfraStack stack)
        {
            Stack = stack;
        }

        public void CreateResources()
        {
            CreatePhoneParameters();
            CreateTwilioParameters();
        }
        #endregion

        #region Phone
        private void CreatePhoneParameters()
        {
            Parameter sourcePhoneNumber = new Parameter("SourcePhoneNumber", new ParameterArgs
            {
                Name = "/WakerUpper/SourcePhoneNumber",
                Type = "SecureString",
                Value = FakePhoneNumber,
                AllowedPattern = PhoneNumberRegex,
            });
            Stack.SourcePhoneNumberParameterName = sourcePhoneNumber.Name;
            
            Parameter targetPhoneNumber = new Parameter("TargetPhoneNumber", new ParameterArgs
            {
                Name = "/WakerUpper/TargetPhoneNumber",
                Type = "SecureString",
                Value = FakePhoneNumber,
                AllowedPattern = PhoneNumberRegex,
            });
            Stack.TargetPhoneNumberParameterName = targetPhoneNumber.Name;
            
            Parameter message = new Parameter("Message", new ParameterArgs
            {
                Name = "/WakerUpper/Message",
                Type = "String",
                Value = "Wake up!",
            });
            Stack.MessageParameterName = message.Name;
        }
        #endregion
        
        #region Twilio
        private void CreateTwilioParameters()
        {
            Parameter accountSid = new Parameter("TwilioAccountSid", new ParameterArgs
            {
                Name = "/WakerUpper/TwilioAccountSid",
                Type = "SecureString",
                Value = Config.RequireSecret("twilioAccountSid"),
            });
            Stack.TwilioAccountSidParameterName = accountSid.Name;
            
            Parameter authToken = new Parameter("TwilioAuthToken", new ParameterArgs
            {
                Name = "/WakerUpper/TwilioAuthToken",
                Type = "SecureString",
                Value = Config.RequireSecret("twilioAuthToken"),
            });
            Stack.TwilioAuthTokenParameterName = authToken.Name;
        }
        #endregion
    }
}
