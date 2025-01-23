using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Net.Mail;
using Abp.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PostmarkDotNet;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

/// <summary>
/// The Postmark-based email sender implementation
/// </summary>
public class PostmarkEmailSender : EmailSenderBase
{
    private readonly IPostmarkClientBuilder _clientBuilder;

    public PostmarkEmailSender(
        IEmailSenderConfiguration configuration,
        IPostmarkClientBuilder clientBuilder) : base(configuration)
    {
        _clientBuilder = clientBuilder;
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    protected override void SendEmail(MailMessage mail)
    {
        AsyncHelper.RunSync(() => SendEmailAsync(mail));
    }

    protected override async Task SendEmailAsync(MailMessage mail)
    {
        var client = _clientBuilder.Build();

        try
        {
            Logger.LogDebug("Preparing to send email to {EmailAddressList}",
                string.Join(", ", mail.To.Select(x => x.Address)));

            if (IsTemplatedEmail(mail))
            {
                await SendTemplatedEmailAsync(client, mail);
            }
            else
            {
                await SendBasicEmailAsync(client, mail);
            }

            Logger.LogInformation("Successfully sent email to {EmailAddressList}",
                string.Join(", ", mail.To.Select(x => x.Address)));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send email to {EmailAddressList}: {ExceptionMessage}",
                string.Join(", ", mail.To.Select(x => x.Address)),
                ex.Message);
            throw;
        }
    }

    private static object? ExtractTemplateModel(MailMessage mail)
    {
        if (string.IsNullOrWhiteSpace(mail.Body))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(mail.Body);
        }
        catch (Exception ex)
        {
            throw new AbpException("Failed to deserialize template model from email body.", ex);
        }
    }

    /// <summary>
    /// Checks the mail message headers to determine if Postmark template information was provided
    /// </summary>
    /// <param name="mail">
    /// The mail message to check
    /// </param>
    /// <returns>
    /// True if the mail message is a templated email, false otherwise
    /// </returns>
    private static bool IsTemplatedEmail(MailMessage mail)
    {
        return mail.Headers["X-Postmark-Template-Id"] != null ||
               mail.Headers["X-Postmark-Template-Alias"] != null;
    }

    private async Task<List<PostmarkMessageAttachment>?> CreateAttachmentsAsync(AttachmentCollection? attachments)
    {
        if (attachments == null || attachments.Count == 0)
        {
            return null;
        }

        Logger.LogDebug("Processing {AttachmentCount} attachments", attachments.Count);
        var postmarkAttachments = new List<PostmarkMessageAttachment>();

        foreach (var attachment in attachments)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await attachment.ContentStream.CopyToAsync(memoryStream);

                postmarkAttachments.Add(new PostmarkMessageAttachment
                {
                    Name = attachment.Name,
                    Content = Convert.ToBase64String(memoryStream.ToArray()),
                    ContentType = attachment.ContentType.MediaType
                });

                Logger.LogDebug("Processed attachment: {AttachmentName} ({ContentType})",
                    attachment.Name,
                    attachment.ContentType.MediaType);

                if (attachment.ContentStream.CanSeek)
                {
                    attachment.ContentStream.Position = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process attachment {AttachmentName}: {ErrorMessage}",
                    attachment.Name,
                    ex.Message);
                throw;
            }
        }

        return postmarkAttachments;
    }

    private async Task<PostmarkMessage> CreateBasicMessageAsync(MailMessage mail)
    {
        Logger.LogDebug("Creating basic message with {AttachmentCount} attachments",
            mail.Attachments.Count);

        var abpConfig = IocManager.Instance.Resolve<IAbpPostmarkConfiguration>();
        var defaultFromAddress = abpConfig.DefaultFromAddress ?? Configuration.DefaultFromAddress;
        var message = new PostmarkMessage
        {
            From = defaultFromAddress ?? mail.From?.Address,
            To = string.Join(",", mail.To.Select(x => x.Address)),
            Subject = mail.Subject,
            TextBody = !mail.IsBodyHtml ? mail.Body : null,
            HtmlBody = mail.IsBodyHtml ? mail.Body : null,
            Cc = mail.CC.Count > 0 ? string.Join(",", mail.CC.Select(x => x.Address)) : null,
            Bcc = mail.Bcc.Count > 0 ? string.Join(",", mail.Bcc.Select(x => x.Address)) : null,
            Attachments = await CreateAttachmentsAsync(mail.Attachments),
            TrackOpens = abpConfig.TrackOpens
        };

        return message;
    }

    private async Task<TemplatedPostmarkMessage> CreateTemplatedMessageAsync(MailMessage mail)
    {
        var templateIdHeader = mail.Headers["X-Postmark-Template-Id"];
        var templateAliasHeader = mail.Headers["X-Postmark-Template-Alias"];

        Logger.LogDebug("Creating templated message. Template ID: {TemplateId}, Template Alias: {TemplateAlias}",
            templateIdHeader ?? string.Empty,
            templateAliasHeader ?? string.Empty);

        var abpConfig = IocManager.Instance.Resolve<IAbpPostmarkConfiguration>();

        var templateId = !string.IsNullOrEmpty(templateIdHeader)
            ? Convert.ToInt64(templateIdHeader)
            : (long?)null;

        var message = new TemplatedPostmarkMessage
        {
            From = mail.From?.Address ?? Configuration.DefaultFromAddress,
            To = string.Join(",", mail.To.Select(x => x.Address)),
            Cc = mail.CC.Count > 0 ? string.Join(",", mail.CC.Select(x => x.Address)) : null,
            Bcc = mail.Bcc.Count > 0 ? string.Join(",", mail.Bcc.Select(x => x.Address)) : null,
            TemplateId = templateId,
            TemplateAlias = templateAliasHeader,
            TemplateModel = ExtractTemplateModel(mail),
            Attachments = await CreateAttachmentsAsync(mail.Attachments),
            TrackOpens = abpConfig.TrackOpens
        };

        return message;
    }

    /// <summary>
    /// Converts the mail message into a non-templated Postmark email message and sends it
    /// </summary>
    /// <param name="client">The Postmark client to use for sending the email</param>
    /// <param name="mail">
    /// The mail message to send. The message must not have an 'X-Postmark-Template-Id' or 'X-Postmark-Template-Alias' header
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the response from the Postmark API
    /// </returns>
    /// <exception cref="AbpException">
    /// Thrown if the Postmark API returns an error status
    /// </exception>
    private async Task SendBasicEmailAsync(IPostmarkClientWrapper client, MailMessage mail)
    {
        var message = await CreateBasicMessageAsync(mail);

        Logger.LogDebug("Sending basic email with subject: {Subject}", mail.Subject);

        var response = await client.SendMessageAsync(message);

        if (response.Status != PostmarkStatus.Success)
        {
            Logger.LogError("Postmark API error: {ErrorMessage}", response.Message);
            throw new AbpException($"Failed to send email via Postmark: {response.Message}");
        }

        Logger.LogDebug("Basic email sent successfully. Message ID: {MessageId}", response.MessageID);
    }

    /// <summary>
    /// Converts the mail message into a templated Postmark email message and sends it
    /// </summary>
    /// <param name="client">The Postmark client to use for sending the email</param>
    /// <param name="mail">
    /// The mail message to send. The message must have either an 'X-Postmark-Template-Id' or 'X-Postmark-Template-Alias' header
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the response from the Postmark API
    /// </returns>
    /// <exception cref="AbpException">
    /// Thrown if the Postmark API returns an error status
    /// </exception>
    private async Task SendTemplatedEmailAsync(IPostmarkClientWrapper client, MailMessage mail)
    {
        var templateMessage = await CreateTemplatedMessageAsync(mail);

        Logger.LogDebug("Sending templated email using template {TemplateId} or alias '{TemplateAlias}'",
            templateMessage.TemplateId ?? 0,
            templateMessage.TemplateAlias ?? string.Empty);

        var response = await client.SendEmailWithTemplateAsync(templateMessage);

        if (response.Status != PostmarkStatus.Success)
        {
            Logger.LogError("Postmark API error: {ErrorMessage}", response.Message);
            throw new AbpException($"Failed to send templated email via Postmark: {response.Message}");
        }

        Logger.LogDebug("Template email sent successfully. Message ID: {MessageId}", response.MessageID);
    }
}
