using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Oyster.EmailService; 

public class SmtpEmailSender:IEmailSender {
  public Task SendEmailAsync(string email, string subject, string htmlMessage) {

    var smtpClient = new SmtpClient {
      Port = 25,
      Host = "localhost",
      DeliveryMethod =
        SmtpDeliveryMethod.Network,
      UseDefaultCredentials = false
    };
    return smtpClient.SendMailAsync("oyster@localhost", email, subject, htmlMessage);
  }
}
