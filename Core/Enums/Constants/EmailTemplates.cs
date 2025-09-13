namespace datopus.api.Core.Enums.Constants
{
    internal class EmailTemplates : Dictionary<EmailNotificationType, string>
    {
        public EmailTemplates()
        {
            base[EmailNotificationType.WelcomeEmailFreeTrial] = "b169e9cd-28c0-44bb-86cb-a2137602ea06";
            base[EmailNotificationType.WelcomeEmailStartupProgram] = "a699e6ef-eb79-4730-b98a-845e30a4ef20";
        }
    }
}