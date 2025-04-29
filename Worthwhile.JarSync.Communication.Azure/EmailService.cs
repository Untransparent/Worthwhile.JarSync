using Azure;
using Azure.Communication.Email;
using Common.EmailService.Interfaces;

namespace Worthwhile.JarSync.Communication.Azure
{
    public class EmailService : IEmailService
    {
        public void SendEmail(EmailMessageRequest request)
        {
            var emailClient = new EmailClient(request.ConnectionString);

            EmailSendOperation emailSendOperation = emailClient.Send(
                WaitUntil.Completed,
                senderAddress: request.From,
                recipientAddress: request.To,
                subject: request.Subject,
                htmlContent: request.HTMLBody,
                plainTextContent: request.TextBody);
        }
    }
}
