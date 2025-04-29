
namespace Worthwhile.JarSync.Core.Config
{
    public class EmailConfig
    {
        public const string SECTION_NAME = "Email";

        public string Enable { get; set; } = "1";
        public string RelayMethod { get; set; } = "1";
        public string SmtpServer { get; set; } = "";
        public string SmtpPort { get; set; } = "";
        public string SmtpUser { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string EnableSslFrom { get; set; } = "";
        public string WORTHWHILE_COMMUNICATION_SERVICE { get; set; } = "";
        public string WORTHWHILE_NOTIFY_FROM { get; set; } = "";
        public string WORTHWHILE_NOTIFY_TO { get; set; } = "";

        public bool IsEnabled => Enable == "1";
        public ERelayMethod EmailRelayMethod = ERelayMethod.None;

        public EmailConfig()
        {
        }

        public void Initialize()
        {
            if (!IsEnabled) return;

            if (string.IsNullOrWhiteSpace(WORTHWHILE_NOTIFY_FROM))
            {
                throw new Exception("WORTHWHILE_NOTIFY_FROM is not set");
            }
            if (string.IsNullOrWhiteSpace(WORTHWHILE_NOTIFY_TO))
            {
                throw new Exception("WORTHWHILE_NOTIFY_TO is not set");
            }

            EmailRelayMethod = RelayMethod switch
            {
                "1" => ERelayMethod.AzureCS,
                "2" => ERelayMethod.SMTP,
                "3" => ERelayMethod.AWS,
                "4" => ERelayMethod.SendGrid,
                "5" => ERelayMethod.MailGun,
                "6" => ERelayMethod.Twilio,
                _ => ERelayMethod.None
            };

            if (EmailRelayMethod == ERelayMethod.AzureCS)
            {
                if (string.IsNullOrWhiteSpace(WORTHWHILE_COMMUNICATION_SERVICE))
                { 
                    throw new Exception("Email__WORTHWHILE_COMMUNICATION_SERVICE is not set");
                }
            }
        }
    }

    public enum ERelayMethod
    {
        None = 0,
        AzureCS = 1,
        SMTP = 2,
        AWS = 3,
        SendGrid = 4,
        MailGun = 5,
        Twilio = 6
    }
}
