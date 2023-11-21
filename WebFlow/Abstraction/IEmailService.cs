using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using WebFlow.Extensions;

namespace WebFlow.Email;

//ToDO add support for default templates
public interface IEmailService
{
    /// <summary>
    /// Sends out an email to the recipient
    /// </summary>
    /// <param name="sender">Account the email is being sent from</param>
    /// <param name="recipientEmail">Person you're trying to send to</param>
    /// <param name="subject"></param>
    /// <param name="body"></param>
    /// <param name="isHtml">Whether the body is html or not</param>
    /// <returns></returns>
    Result SendOutEmail(MailboxAddress sender, [EmailAddress] string recipientEmail, string subject, string body, bool isHtml = false);
    
    /// <summary>
    /// Send a templated email to the recipient, requires an html object and a object with the mapped variables
    /// </summary>
    /// <param name="sender">Account the email is being sent from</param>
    /// <param name="recipientEmail">Person you're trying to send to</param>
    /// <param name="subject"></param>
    /// <param name="templateObject">The object that contains the variables which can be found in the html file</param>
    /// <param name="htmlContent">The html content</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Result SendOutEmail<T>(MailboxAddress sender, [EmailAddress] string recipientEmail, string subject, T templateObject, string htmlContent);

    /// <summary>
    /// Sends out an email asynchronously to the recipient
    /// </summary>
    /// <param name="sender">Account the email is being sent from</param>
    /// <param name="recipientEmail">Person you're trying to send to</param>
    /// <param name="subject"></param>
    /// <param name="body"></param>
    /// <param name="isHtml">Whether the body is html or not</param>
    /// <returns></returns>
    Task<Result> SendOutEmailAsync(MailboxAddress sender, string recipientEmail, string subject, string body, bool isHtml = false);
    
    /// <summary>
    /// Send a templated email asynchronously to the recipient, requires an html object and a object with the mapped variables
    /// </summary>
    /// <param name="sender">Account the email is being sent from</param>
    /// <param name="recipientEmail">Person you're trying to send to</param>
    /// <param name="subject"></param>
    /// <param name="templateObject">The object that contains the variables which can be found in the html file</param>
    /// <param name="htmlContent">The html content</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result> SendOutEmailAsync<T>(MailboxAddress sender, string recipientEmail, string subject, T templateObject, string htmlContent);
}

public static partial class RegisterWebFlowServices
{
    public static void RegisterEmailService(this IServiceCollection serviceCollection, string connectionString)
    {
        string[] splitConnectionString = connectionString.Split(':');
        int port = Convert.ToInt32(splitConnectionString[1]);
        
        serviceCollection.AddTransient(typeof(IEmailService), typeof(EmailImplementation));
        serviceCollection.AddTransient(_ =>
        {
            var smtpClient = new SmtpClient();
            smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
            smtpClient.Connect(splitConnectionString[0], port, SecureSocketOptions.StartTls);
            
            return smtpClient;
        });
    }
}