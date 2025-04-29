
namespace Common.EmailService.Interfaces
{
    public interface IEmailService
    {
        void SendEmail(EmailMessageRequest request);
    }

    public class EmailMessageRequest
    { 
        public string ConnectionString { get; set; }
        public string Subject { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string HTMLBody { get; set; }
        public string TextBody { get; set; }
    }
}
