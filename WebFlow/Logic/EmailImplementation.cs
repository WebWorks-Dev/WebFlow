using System.Reflection;
using MailKit.Net.Smtp;
using MimeKit;
using WebFlow.Extensions;

namespace WebFlow.Email;

internal class EmailImplementation : IEmailService
{
    private readonly SmtpClient _smtpClient;

    public EmailImplementation(SmtpClient smtpClient)
    {
        _smtpClient = smtpClient;
    }

    private MimeMessage CreateMimeMessage(MailboxAddress sender, string recipientEmail, string subject, string body, bool isHtml = false)
    {
        var message = new MimeMessage();
        message.From.Add(sender);
        message.To.Add(new MailboxAddress("", recipientEmail));
        message.Subject = subject;
        
        var builder = new BodyBuilder();
        if (isHtml)
            builder.HtmlBody = body;
        else
            builder.TextBody = body;
        
        message.Body = builder.ToMessageBody();

        return message;
    }

    private void ConvertTemplate<T>(T templateObject, ref string html)
    {
        if (templateObject is null)
            return;
        
        PropertyInfo[] templateProperties = templateObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (templateProperties.Length is 0)
            return;

        foreach (var property in templateProperties)
        {
            object? propertyValue = property.GetValue(templateObject);

            if (propertyValue is not null)
                html = html.Replace(property.Name, propertyValue.ToString());
        }
    }
    
    public Result SendOutEmail(MailboxAddress sender, string recipientEmail, string subject, string body, bool isHtml = false)
    {
        var mimeMessage = CreateMimeMessage(sender, recipientEmail, subject, body, isHtml);

        return ExceptionExtensions.Try(() =>
        {
            _smtpClient.Send(mimeMessage);
        });
    }

    public Result SendOutEmail<T>(MailboxAddress sender, string recipientEmail, string subject, T templateObject, string htmlContent)
    {
        ConvertTemplate(templateObject, ref htmlContent);
        
        var mimeMessage = CreateMimeMessage(sender, recipientEmail, subject, htmlContent, true);
        
        return ExceptionExtensions.Try(() =>
        {
            _smtpClient.Send(mimeMessage);
        });
    }
    
    public async Task<Result> SendOutEmailAsync(MailboxAddress sender, string recipientEmail, string subject, string body, bool isHtml = false)
    {
        var mimeMessage = CreateMimeMessage(sender, recipientEmail, subject, body, isHtml);

        return await ExceptionExtensions.TryAsync(async () =>
        {
            await _smtpClient.SendAsync(mimeMessage);
        });
    }
    
    public async Task<Result> SendOutEmailAsync<T>(MailboxAddress sender, string recipientEmail, string subject, T templateObject, string htmlContent)
    {
        ConvertTemplate(templateObject, ref htmlContent);
        
        var mimeMessage = CreateMimeMessage(sender, recipientEmail, subject, htmlContent, true);

        return await ExceptionExtensions.TryAsync(async () =>
        {
            await _smtpClient.SendAsync(mimeMessage);
        });
    }
}