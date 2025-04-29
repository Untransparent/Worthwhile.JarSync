
using Common.EmailService.Interfaces;
using Worthwhile.JarSync.Core.Config;
using System.Text;

namespace Worthwhile.JarSync.Core.Interfaces
{
    public interface IEmailProcessor
    {
        void SendEmail(JarSyncOperationResult result);
        void SendEmail(Exception exc);
    }

    public class EmailProcessor : IEmailProcessor
    {
        private IEmailService mService;
        private EmailConfig mConfig;

        public EmailProcessor(IEmailService service, EmailConfig config)
        {
            mService = service;
            mConfig = config;
        }

        public void SendEmail(JarSyncOperationResult result)
        {
            EmailMessageRequest request = result.TotalErrors > 0 ? GetFailEmailContent(result) : GetSuccessEmailContent(result);
            mService.SendEmail(request);
        }

        public void SendEmail(Exception exc)
        {
            EmailMessageRequest request = GetFailEmailContent(exc);
            mService.SendEmail(request);
        }

        private EmailMessageRequest GetSuccessEmailContent(JarSyncOperationResult result)
        {
            EmailMessageRequest request = BuildEmailRequestConfig();

            request.Subject = "Worthwhile utility sync completed successfully";

            StringBuilder html = new StringBuilder();
            StringBuilder text = new StringBuilder();
            html.AppendLine("<html><h1>Detail statistics:</h1><br/>");
            text.AppendLine("Detail statistics:");

            html.AppendLine($"<h2>Number of folders created: {result.TotalFoldersCreated}</h2><br/>");
            text.AppendLine($"Number of folders created: {result.TotalFoldersCreated}");

            html.AppendLine($"<h2>Number of folders deleted: {result.TotalFoldersDeleted}</h2><br/>");
            text.AppendLine($"Number of folders deleted: {result.TotalFoldersDeleted}");

            html.AppendLine($"<h2>Number of files created: {result.TotalFilesNew}</h2><br/>");
            text.AppendLine($"Number of files created: {result.TotalFilesNew}");

            html.AppendLine($"<h2>Number of files updated: {result.TotalFilesUpdated}</h2><br/>");
            text.AppendLine($"Number of files updated: {result.TotalFilesUpdated}");

            html.AppendLine($"<h2>Number of files deleted: {result.TotalFilesDeleted}</h2><br/>");
            text.AppendLine($"Number of files deleted: {result.TotalFilesDeleted}");

            html.AppendLine("</html>");

            request.HTMLBody = html.ToString();
            request.TextBody = text.ToString();

            return request;
        }

        private EmailMessageRequest GetFailEmailContent(JarSyncOperationResult result)
        {
            EmailMessageRequest request = BuildEmailRequestConfig();

            request.Subject = "FileSyncError: Worthwhile utility sync failed!!!";

            StringBuilder html = new StringBuilder();
            StringBuilder text = new StringBuilder();
            html.AppendLine($"<html><h1>Errors ({result.TotalErrors}) occurred during syncronization:</h1><br/>");
            text.AppendLine("Errors occurred during syncronization:");

            int maxCount = 10;
            string[] errors = result.GetErrors();
            foreach (string error in errors)
            {
                html.AppendLine($"<h2>{error}</h2><br/>");
                text.AppendLine(error);
                maxCount++;
            }
            html.AppendLine("</html>");

            request.HTMLBody = html.ToString();
            request.TextBody = text.ToString();

            return request;
        }

        private EmailMessageRequest GetFailEmailContent(Exception aExc)
        {
            EmailMessageRequest request = BuildEmailRequestConfig();

            request.Subject = "UnhandledError: Worthwhile utility sync failed!!!";

            request.HTMLBody = $"<html><h1>UnhandledError occurred:</h1><br/><h2>{aExc.ToString()}</h2></html>";
            request.TextBody = $"UnhandledError occurred:\r\n{aExc.ToString()}";

            return request;
        }

        private EmailMessageRequest BuildEmailRequestConfig()
        {
            EmailMessageRequest request = new EmailMessageRequest
            {
                From = mConfig.WORTHWHILE_NOTIFY_FROM,
                To = mConfig.WORTHWHILE_NOTIFY_TO
            };

            if (mConfig.EmailRelayMethod == ERelayMethod.AzureCS)
            {
                request.ConnectionString = mConfig.WORTHWHILE_COMMUNICATION_SERVICE;
            }

            return request;
        }
    }
}
