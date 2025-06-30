using System.Net.Mail;
using System.Net;
using HMCodingWeb.Models;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static System.Net.WebRequestMethods;

namespace HMCodingWeb.Services
{
    public class EmailSendService
    {
        private readonly OnlineCodingWebContext _context;
        public EmailSendService(OnlineCodingWebContext context)
        {
            _context = context;
        }

        public async Task<bool> SendOtpToEmail(string email, string otp)
        {
            try
            {
                var fromAddress = new MailAddress("haunv.cntt@gmail.com", "HMCoding Website");
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
    
        public async Task<bool> SendNotificationToUser(string title, string content, long[] listUserId)
        {
            var listUser = _context.Users
                .Where(u => listUserId.Contains(u.Id))
                .Select(u => new User() { Id = u.Id, Username = u.Username, Email = u.Email })
                .Where(u => !string.IsNullOrEmpty(u.Email) && u.Email.Contains("@"))
                .ToList();
            foreach(var user in listUser)
            {
                try
                {
                    var fromAddress = new MailAddress("haunv.cntt@gmail.com", "HMCoding Website");
                    var toAddress = new MailAddress(user.Email);
                    const string fromPassword = "edss szph iorj ymbs";
                    string subject = title;
                    string body = content;

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
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        await smtp.SendMailAsync(message);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return true;
        }
    }
}
