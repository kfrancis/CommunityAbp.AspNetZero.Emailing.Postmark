using System;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using Abp;
using Abp.Net.Mail;
using Abp.Threading;
using Microsoft.Extensions.Logging;
using PostmarkDotNet;

namespace CommunityAbp.AspNetZero.Emailing.Postmark
{
    /// <summary>
    /// A testable version of PostmarkEmailSender that exposes protected methods for testing
    /// </summary>
    public class TestablePostmarkEmailSender : PostmarkEmailSender
    {
        private readonly IPostmarkClientWrapper _clientWrapper;

        public TestablePostmarkEmailSender(
            IEmailSenderConfiguration configuration,
            IAbpPostmarkConfiguration abpPostmarkConfiguration,
            IPostmarkClientBuilder clientBuilder,
            IPostmarkClientWrapper clientWrapper)
            : base(configuration, abpPostmarkConfiguration, clientBuilder)
        {
            _clientWrapper = clientWrapper;
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

        // Make the synchronous method public as well
        public new void SendEmail(MailMessage mail)
        {
            AsyncHelper.RunSync(() => SendEmailAsync(mail));
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
    }
}
