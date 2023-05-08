using Bul.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Upsilon.Services.Services
{
    public class DriveService : IDriveService
    {
        protected readonly IConfiguration Configuration;
        private IHttpContextAccessor _httpContextAccessor;
        private string Ip = "";
        public DriveService(IHttpContextAccessor httpContextAccessor)
        {
            var myEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{myEnv}.json", false)
                .Build();
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> DriveSendMail(DriveSendViewModel vm)
        {
            var result = new Result();
            try
            {
                string sender = Configuration.GetSection("DriveMail:email").Value;
                string password = GetPassword(sender.Replace("armaara.com", "bul.com.tr"));
                string smtpServer = Configuration.GetSection("MailSettings:smtpServer").Value;
                int smtpPort = Convert.ToInt32(Configuration.GetSection("MailSettings:smtpPort").Value);

                // create email message
                var mail = new MimeMessage();
                mail.From.Add(MailboxAddress.Parse(sender));

                // add receivers
                foreach (string receiver in vm.Receivers ?? Enumerable.Empty<string>())
                {
                    mail.Bcc.Add(MailboxAddress.Parse(receiver));
                }

                mail.Subject = vm.Subject;
                var builder = new BodyBuilder();
                builder.HtmlBody = vm.Body;


                mail.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(sender, password);
                await smtp.SendAsync(mail);
                await smtp.DisconnectAsync(true);

                result.Success = true;
                result.Message = "Mail sent to " + string.Join(",", vm.Receivers) + " successfuly";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + Configuration.GetSection("DriveMail:email").Value + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        public string GetPassword(string email)
        {
            return AesOperation.EncryptString(Configuration.GetSection("AesCryptKey").Value, email);
        }
    }
}
