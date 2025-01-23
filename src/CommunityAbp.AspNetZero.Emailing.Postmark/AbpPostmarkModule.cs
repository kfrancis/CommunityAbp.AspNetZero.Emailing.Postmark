using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Modules;
using Abp;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Net.Mail;
using Abp.Reflection.Extensions;
using PostmarkDotNet;
using Abp.Threading;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

/// <summary>
/// Module for Postmark email sending integration.
/// </summary>
[DependsOn(typeof(AbpKernelModule))]
public class AbpPostmarkModule : AbpModule
{
    public override void PreInitialize()
    {
        IocManager.Register<IAbpPostmarkConfiguration, AbpPostmarkConfiguration>();
        Configuration.ReplaceService<IEmailSender, PostmarkEmailSender>(DependencyLifeStyle.Transient);
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(AbpPostmarkModule).GetAssembly());
    }
}

/// <summary>
/// Postmark configuration
/// </summary>
public interface IAbpPostmarkConfiguration
{
    public string? ApiKey { get; set; }
    public bool? TrackOpens { get; set; }
}

/// <summary>
/// Postmark configuration
/// </summary>
public class AbpPostmarkConfiguration : IAbpPostmarkConfiguration
{
    public string? ApiKey { get; set; }
    public bool? TrackOpens { get; set; }
}

public interface IPostmarkClientWrapper
{
    Task<PostmarkResponse> SendMessageAsync(PostmarkMessage message);
    Task<PostmarkResponse> SendEmailWithTemplateAsync(TemplatedPostmarkMessage message);
}

public class PostmarkClientWrapper : IPostmarkClientWrapper
{
    private readonly PostmarkClient _client;

    public PostmarkClientWrapper(PostmarkClient client)
    {
        _client = client;
    }

    public Task<PostmarkResponse> SendMessageAsync(PostmarkMessage message)
    {
        return _client.SendMessageAsync(message);
    }

    public Task<PostmarkResponse> SendEmailWithTemplateAsync(TemplatedPostmarkMessage message)
    {
        return _client.SendEmailWithTemplateAsync(message);
    }
}

/// <summary>
/// The Postmark-based email sender implementation
/// </summary>
public class PostmarkEmailSender : EmailSenderBase
{
    public ILogger Logger { get; set; }
    private readonly IPostmarkClientBuilder _clientBuilder;

    public PostmarkEmailSender(
        IEmailSenderConfiguration configuration,
        IPostmarkClientBuilder clientBuilder) : base(configuration)
    {
        _clientBuilder = clientBuilder;
        Logger = NullLogger.Instance;
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

    protected override void SendEmail(MailMessage mail)
    {
        AsyncHelper.RunSync(() => SendEmailAsync(mail));
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

    private async Task<PostmarkMessage> CreateBasicMessageAsync(MailMessage mail)
    {
        Logger.LogDebug("Creating basic message with {AttachmentCount} attachments",
            mail.Attachments.Count);

        var abpConfig = IocManager.Instance.Resolve<IAbpPostmarkConfiguration>();

        var message = new PostmarkMessage
        {
            From = mail.From?.Address ?? Configuration.DefaultFromAddress,
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
}

public static class AbpPostmarkConfigurationExtensions
{
    public static IAbpPostmarkConfiguration AbpPostmark(this IModuleConfigurations configurations)
    {
        return configurations.AbpConfiguration.Get<IAbpPostmarkConfiguration>();
    }
}

public interface IPostmarkClientBuilder
{
    IPostmarkClientWrapper Build();
}

public class DefaultPostmarkClientBuilder : IPostmarkClientBuilder, ITransientDependency
{
    private readonly IAbpPostmarkConfiguration _abpPostmarkConfiguration;

    public DefaultPostmarkClientBuilder(IAbpPostmarkConfiguration abpPostmarkConfiguration)
    {
        _abpPostmarkConfiguration = abpPostmarkConfiguration;
    }

    public virtual IPostmarkClientWrapper Build()
    {
        var client = new PostmarkClient(_abpPostmarkConfiguration.ApiKey);
        ConfigureClient(client);
        return new PostmarkClientWrapper(client);
    }

    protected virtual void ConfigureClient(PostmarkClient client)
    {
    }
}

public static class MailMessageExtensions
{
    public static MailMessage UseTemplate(this MailMessage mail, long templateId, object model)
    {
        mail.Headers.Add("X-Postmark-Template-Id", templateId.ToString());
        mail.Body = System.Text.Json.JsonSerializer.Serialize(model);
        return mail;
    }

    public static MailMessage UseTemplate(this MailMessage mail, string templateAlias, object model)
    {
        mail.Headers.Add("X-Postmark-Template-Alias", templateAlias);
        mail.Body = System.Text.Json.JsonSerializer.Serialize(model);
        return mail;
    }
}

/// <summary>
/// A testable version of PostmarkEmailSender that exposes protected methods for testing
/// </summary>
public class TestablePostmarkEmailSender : PostmarkEmailSender
{
    private readonly IPostmarkClientWrapper _clientWrapper;

    public TestablePostmarkEmailSender(
        IEmailSenderConfiguration configuration,
        IPostmarkClientBuilder clientBuilder,
        IPostmarkClientWrapper clientWrapper)
        : base(configuration, clientBuilder)
    {
        _clientWrapper = clientWrapper;
    }

    // Make the method public while still overriding the protected base method
    public new async Task SendEmailAsync(MailMessage mail)
    {
        try
        {
            if (IsTemplatedEmail(mail))
            {
                var templateMessage = await CreateTemplatedMessageForTesting(mail);
                var response = await _clientWrapper.SendEmailWithTemplateAsync(templateMessage);
                HandleResponse(response);
            }
            else
            {
                var message = await CreateBasicMessageForTesting(mail);
                var response = await _clientWrapper.SendMessageAsync(message);
                HandleResponse(response);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send email to {EmailAddressList}: {ExceptionMessage}",
                string.Join(", ", mail.To.Select(x => x.Address)),
                ex.Message);
            throw;
        }
    }

    // Make the synchronous method public as well
    public new void SendEmail(MailMessage mail)
    {
        AsyncHelper.RunSync(() => SendEmailAsync(mail));
    }

    private void HandleResponse(PostmarkResponse response)
    {
        if (response.Status != PostmarkStatus.Success)
        {
            throw new AbpException($"Failed to send email via Postmark: {response.Message}");
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "<Pending>")]
    private bool IsTemplatedEmail(MailMessage mail)
    {
        var methodInfo = typeof(PostmarkEmailSender).GetMethod(
            "IsTemplatedEmail",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (methodInfo == null)
        {
            throw new InvalidOperationException(
                "Could not find IsTemplatedEmail method. The method might have been renamed or removed.");
        }

        var result = methodInfo.Invoke(null, [mail]);
        return (bool)(result ?? throw new InvalidOperationException(
            "IsTemplatedEmail method returned null, which is unexpected for a boolean method."));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "<Pending>")]
    public async Task<PostmarkMessage> CreateBasicMessageForTesting(MailMessage mail)
    {
        var methodInfo = typeof(PostmarkEmailSender).GetMethod(
            "CreateBasicMessageAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (methodInfo == null)
        {
            throw new InvalidOperationException(
                "Could not find CreateBasicMessageAsync method. The method might have been renamed or removed.");
        }

        try
        {
            var task = (Task<PostmarkMessage>?)methodInfo.Invoke(this, [mail]);
            if (task == null)
            {
                throw new InvalidOperationException(
                    "CreateBasicMessageAsync method returned null, which is unexpected for an async method.");
            }

            return await task;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "<Pending>")]
    public async Task<TemplatedPostmarkMessage> CreateTemplatedMessageForTesting(MailMessage mail)
    {
        var methodInfo = typeof(PostmarkEmailSender).GetMethod(
            "CreateTemplatedMessageAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (methodInfo == null)
        {
            throw new InvalidOperationException(
                "Could not find CreateTemplatedMessageAsync method. The method might have been renamed or removed.");
        }

        try
        {
            var task = (Task<TemplatedPostmarkMessage>?)methodInfo.Invoke(this, [mail]);
            if (task == null)
            {
                throw new InvalidOperationException(
                    "CreateTemplatedMessageAsync method returned null, which is unexpected for an async method.");
            }

            return await task;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }
}
