using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Numerics;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Net.Mail;
using Abp.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PostmarkDotNet;
using PostmarkDotNet.Model;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

/// <summary>
/// The Postmark-based email sender implementation
/// </summary>
public class PostmarkEmailSender : EmailSenderBase
{
    private readonly IAbpPostmarkConfiguration _abpPostmarkConfiguration;
    private readonly IPostmarkClientBuilder _clientBuilder;
    public const string TemplateIdHeader = "X-Postmark-Template-Id";
    public const string TemplateAliasHeader = "X-Postmark-Template-Alias";
    public const string TagHeader = "X-Postmark-Tag";
    public const string TrackLinksHeader = "X-Postmark-TrackLinks";

    public PostmarkEmailSender(
        IEmailSenderConfiguration configuration,
        IAbpPostmarkConfiguration abpPostmarkConfiguration,
        IPostmarkClientBuilder clientBuilder) : base(configuration)
    {
        _abpPostmarkConfiguration = abpPostmarkConfiguration;
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
        if (mail == null)
        {
            throw new ArgumentNullException(nameof(mail));
        }

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
        if (mail == null)
        {
            throw new ArgumentNullException(nameof(mail));
        }

        return mail.Headers[TemplateIdHeader] != null ||
               mail.Headers[TemplateAliasHeader] != null;
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
        ValidateMail(mail);

        Logger.LogDebug("Creating basic message with {AttachmentCount} attachments",
            mail.Attachments.Count);

        var defaultFromAddress = _abpPostmarkConfiguration.DefaultFromAddress ?? Configuration.DefaultFromAddress;
        var message = new PostmarkMessage
        {
            From = defaultFromAddress ?? mail.From?.Address,
            To = string.Join(",", mail.To.Select(x => x.Address)),
            Subject = mail.Subject,
            TextBody = !mail.IsBodyHtml ? mail.Body : null,
            HtmlBody = mail.IsBodyHtml ? mail.Body : null,
            Cc = mail.CC.Count > 0 ? string.Join(",", mail.CC.Select(x => x.Address)) : null,
            Bcc = mail.Bcc.Count > 0 ? string.Join(",", mail.Bcc.Select(x => x.Address)) : null,
            ReplyTo = mail.ReplyToList.Count > 0 ? string.Join(",", mail.ReplyToList.Select(x => x.Address)) : null,
            Attachments = await CreateAttachmentsAsync(mail.Attachments),
            Tag = mail.Headers[TagHeader],
            TrackOpens = _abpPostmarkConfiguration.TrackOpens
        };

        if (bool.TryParse(mail.Headers[TrackLinksHeader], out var shouldTrack))
        {
            message.TrackLinks = shouldTrack ? LinkTrackingOptions.HtmlAndText : LinkTrackingOptions.None;
        }

        // If there are any headers in the mail message, convert them and add them to the postmark message
        if (mail.Headers.Count <= 0)
        {
            return message;
        }

        Logger.LogDebug("Processing {HeaderCount} headers", mail.Headers.Count);
        var headers = new HeaderCollection();
        foreach (var headerKey in mail.Headers.AllKeys.Except([TemplateIdHeader, TemplateAliasHeader]))
        {
            headers.Add(new MailHeader(headerKey, mail.Headers[headerKey]));
            Logger.LogDebug("Added header: {HeaderKey} = {HeaderValue}",
                headerKey,
                mail.Headers[headerKey]);
        }
        message.Headers = headers;

        return message;
    }

    private async Task<TemplatedPostmarkMessage> CreateTemplatedMessageAsync(MailMessage mail)
    {
        ValidateMail(mail);

        var templateIdHeader = mail.Headers[TemplateIdHeader];
        var templateAliasHeader = mail.Headers[TemplateAliasHeader];

        Logger.LogDebug("Creating templated message. Template ID: {TemplateId}, Template Alias: {TemplateAlias}",
            templateIdHeader ?? string.Empty,
            templateAliasHeader ?? string.Empty);

        var templateId = !string.IsNullOrEmpty(templateIdHeader)
            ? Convert.ToInt64(templateIdHeader)
            : (long?)null;

        var defaultFromAddress = _abpPostmarkConfiguration.DefaultFromAddress ?? Configuration.DefaultFromAddress;

        var message = new TemplatedPostmarkMessage
        {
            From = defaultFromAddress ?? mail.From?.Address,
            To = string.Join(",", mail.To.Select(x => x.Address)),
            Cc = mail.CC.Count > 0 ? string.Join(",", mail.CC.Select(x => x.Address)) : null,
            Bcc = mail.Bcc.Count > 0 ? string.Join(",", mail.Bcc.Select(x => x.Address)) : null,
            ReplyTo = mail.ReplyToList.Count > 0 ? string.Join(",", mail.ReplyToList.Select(x => x.Address)) : null,
            TemplateId = templateId,
            TemplateAlias = templateAliasHeader,
            TemplateModel = ExtractTemplateModel(mail),
            Attachments = await CreateAttachmentsAsync(mail.Attachments),
            TrackOpens = _abpPostmarkConfiguration.TrackOpens,
            Tag = mail.Headers[TagHeader]
        };

        if (bool.TryParse(mail.Headers[TrackLinksHeader], out var shouldTrack))
        {
            message.TrackLinks = shouldTrack ? LinkTrackingOptions.HtmlAndText : LinkTrackingOptions.None;
        }

        Logger.LogDebug("Processing {HeaderCount} headers", mail.Headers.Count);
        var headers = new HeaderCollection();
        foreach (var headerKey in mail.Headers.AllKeys.Except([TemplateIdHeader, TemplateAliasHeader]))
        {
            headers.Add(new MailHeader(headerKey, mail.Headers[headerKey]));
            Logger.LogDebug("Added header: {HeaderKey} = {HeaderValue}",
                headerKey,
                mail.Headers[headerKey]);
        }
        message.Headers = headers;

        return message;
    }

    private static void ValidateMail(MailMessage mail)
    {
        // Make sure there are the basics required for sending, like a destination
        if (mail.To.Count == 0)
        {
            throw new AbpException("No destination address specified in the email");
        }
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
