using System.Net.Mail;
using System.Net;

namespace HMCodingWeb.Services
{
    public class EmailSendService
    {
        public async Task<bool> SendOtpToEmail(string email, string otp)
        {
            try
            {
                var fromAddress = new MailAddress("haunv.cntt@gmail.com", "Online Coding Website");
                var toAddress = new MailAddress(email);
                const string fromPassword = "edss szph iorj ymbs";
                const string subject = "Verify Email address";
                string body = $@"Greeting,

To complete the register, enter the verification code on the unrecognized device. 
Verification code is: {otp}
If you did not attempt to sign in to your account, your password may be compromised. Answer this email to get our support soon.

Thanks,
Website's Author";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    await smtp.SendMailAsync(message);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
