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
    /// Sends an email to a recipient.
    /// </summary>
    /// <param name="sender">The mailbox address from which the email will be sent.</param>
    /// <param name="recipientEmail">Email address of the recipient.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The main content of the email.</param>
    /// <param name="isHtml">Flag indicating whether the body content is HTML or not. (default to false)</param>
    /// <returns>A result object indicating the status of the email sending operation.</returns>
    Result SendOutEmail(MailboxAddress sender, [EmailAddress] string recipientEmail, string subject, string body, bool isHtml = false);
    
    /// <summary>
    /// Sends a templated email to a recipient.
    /// </summary>
    /// <param name="sender">The mailbox address from which the email will be sent.</param>
    /// <param name="recipientEmail">Email address of the recipient.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="templateObject">An object that contains variables to be replaced in the HTML template.</param>
    /// <param name="htmlContent">The HTML template for the email body.</param>
    /// <typeparam name="T">The type of the template object.</typeparam>
    /// <returns>A result object indicating the status of the email sending operation.</returns>
    Result SendOutEmail<T>(MailboxAddress sender, [EmailAddress] string recipientEmail, string subject, T templateObject, string htmlContent);

    /// <summary>
    /// Asynchronously sends an email to a recipient.
    /// </summary>
    /// <param name="sender">The mailbox address from which the email will be sent.</param>
    /// <param name="recipientEmail">Email address of the recipient.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The main content of the email.</param>
    /// <param name="isHtml">Flag indicating whether the body content is HTML or not. (default is to false)</param>
    /// <returns>A task representing the asynchronous email operation, containing a result object indicating the status of the email sending operation.</returns>
    Task<Result> SendOutEmailAsync(MailboxAddress sender, string recipientEmail, string subject, string body, bool isHtml = false);
    
    /// <summary>
    /// Asynchronously sends a templated email to a recipient.
    /// </summary>
    /// <param name="sender">The mailbox address from which the email will be sent.</param>
    /// <param name="recipientEmail">Email address of the recipient.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="templateObject">An object that contains variables to be replaced in the HTML template.</param>
    /// <param name="htmlContent">The HTML template for the email body.</param>
    /// <typeparam name="T">The type of the template object.</typeparam>
    /// <returns>A task representing the asynchronous email operation, containing a result object indicating the status of the email sending operation.</returns>
    Task<Result> SendOutEmailAsync<T>(MailboxAddress sender, string recipientEmail, string subject, T templateObject, string htmlContent);
}

public static partial class RegisterWebFlowServices
{
    public static void RegisterEmailService(this IServiceCollection serviceCollection, string connectionString, string email, string password)
    {
        string[] splitConnectionString = connectionString.Split(':');
        int port = Convert.ToInt32(splitConnectionString[1]);
        
        serviceCollection.AddTransient(typeof(IEmailService), typeof(EmailImplementation));
        serviceCollection.AddTransient(_ =>
        {
            var smtpClient = new SmtpClient();
            smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
            smtpClient.Connect(splitConnectionString[0], port, SecureSocketOptions.StartTls);
            smtpClient.Authenticate(email, password);

            return smtpClient;
        });
    }
}