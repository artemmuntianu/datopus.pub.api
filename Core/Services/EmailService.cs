using System.Net.Http.Headers;
using datopus.api.Core.Enums;
using datopus.api.Core.Enums.Constants;
using datopus.Core.Exceptions;

namespace datopus.api.Core.Services
{
    internal class EmailService
    {
        private const string MailtrapApiAccessToken = "5cc9cff8175823a19a96e9cc1c20b5b6";
        private static readonly EmailTemplates EmailTemplates = [];

        public static async Task SendMesssage(
            string to,
            string toName,
            EmailNotificationType notificationType
        )
        {
#if DEBUG
            to = "artem@datopus.io";
#endif

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                MailtrapApiAccessToken
            );

            var emailData = new
            {
                from = new { email = "noreply@datopus.io", name = "Datopus" },
                to = new[] { new { email = to, name = toName } },
                reply_to = new { email = "artem@datopus.io", name = "Datopus Support" },
                template_uuid = EmailTemplates[notificationType],
                template_variables = new
                {
                    first_name = toName,
                    company_info_name = "Datopus",
                    company_info_address = "Avenida Cidade de Maringá 55",
                    company_info_city = "Leiria",
                    company_info_zip_code = "2400-137",
                    company_info_country = "Portugal",
                },
            };

            var response = await client.PostAsJsonAsync(
                "https://send.api.mailtrap.io/api/send",
                emailData
            );
        }

        public static async Task SendSupportRequest(
            string userEmail,
            bool? isEmailVerified,
            string? userName,
            string? orgName,
            string subject,
            string message,
            bool allowProjectSupport,
            IFormFile[]? files
        )
        {
#if DEBUG
            const string supportEmail = "artem@datopus.io";
#else
            const string supportEmail = "support@datopus.io";
#endif

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                MailtrapApiAccessToken
            );

            var attachments = new List<object>();

            if (files != null)
            {
                foreach (var f in files)
                {
                    var base64Content = await f.ToBase64Async();

                    attachments.Add(
                        new
                        {
                            filename = f.FileName,
                            content = base64Content,
                            type = f.ContentType,
                            content_id = $"image{attachments.Count + 1}",
                        }
                    );
                }
            }

            var emailData = new
            {
                // NOTE: maybe we need to register additiona account like support.system@datopus.io
                from = new { email = "noreply@datopus.io", name = "Datopus Support System" },
                to = new[] { new { email = supportEmail, name = "Datopus Support Team" } },
                reply_to = new { email = userEmail, name = userName },
                subject = $"Support Request from {userName}: {subject}",
                text = $@"
Support Request

User Name : {userName}
User Email: {userEmail}
Email Verified: {isEmailVerified}
User Org Name: {orgName}
Subject   : {subject}
Allow project support  : {allowProjectSupport}

Message:
{message}",
                html = $@"
<h2>Support Request</h2>
<p><strong>User Name:</strong> {userName}</p>
<p><strong>User Email:</strong> <a href='mailto:{userEmail}'>{userEmail}</a></p>
<p><strong>Email Verified:</strong> {isEmailVerified}</p>
<p><strong>User Org Name:</strong> {orgName}</p>
<p><strong>Subject:</strong> {subject}</p>
<p><strong>Allow Project Support:</strong> {allowProjectSupport}</p>
<p><strong>Message:</strong><br>{System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br>")}</p>",
                attachments,
            };

            var response = await client.PostAsJsonAsync(
                "https://send.api.mailtrap.io/api/send",
                emailData
            );

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var errorResponse =
                            await response.Content.ReadFromJsonAsync<MailTrapErrorResponse>();
                        throw new EmailServiceException("Mail Trap Error", errorResponse?.errors);
                    }
                    catch
                    {
                        throw new EmailServiceException("Mail Trap Error");
                    }
                }
                else
                {
                    throw new EmailServiceException("Mail Trap Error");
                }
            }

            response.EnsureSuccessStatusCode();
        }
    }
}
