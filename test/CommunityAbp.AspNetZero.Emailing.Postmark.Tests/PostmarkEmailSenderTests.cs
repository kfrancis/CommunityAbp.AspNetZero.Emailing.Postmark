using System.Net.Mail;
using Abp.Configuration.Startup;
using Abp.Net.Mail;
using Abp.TestBase;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PostmarkDotNet;
using Shouldly;
using Attachment = System.Net.Mail.Attachment;

namespace CommunityAbp.AspNetZero.Emailing.Postmark.Tests
{
    public class PostmarkEmailSenderTests : AbpIntegratedTestBase<AbpPostmarkModule>
    {
        private readonly IPostmarkClientBuilder _mockClientBuilder;
        private readonly IPostmarkClientWrapper _mockClientWrapper;
        private readonly IEmailSenderConfiguration _mockConfiguration;
        private readonly ILogger _mockLogger;
        private readonly TestablePostmarkEmailSender _sut;
        private readonly IAbpPostmarkConfiguration _mockPostmarkConfiguration;

        public PostmarkEmailSenderTests()
        {
            // Mock dependencies
            _mockClientWrapper = Substitute.For<IPostmarkClientWrapper>();
            _mockClientBuilder = Substitute.For<IPostmarkClientBuilder>();
            _mockConfiguration = Substitute.For<IEmailSenderConfiguration>();
            _mockLogger = Substitute.For<ILogger>();
            _mockPostmarkConfiguration = Substitute.For<IAbpPostmarkConfiguration>();

            // Setup configuration
            _mockConfiguration.DefaultFromAddress.Returns("test@example.com");
            _mockPostmarkConfiguration.ApiKey.Returns("test-api-key");
            _mockPostmarkConfiguration.TrackOpens.Returns(true);

            // Register dependencies in IoC container
            LocalIocManager.IocContainer.Register(
                Component.For<IAbpPostmarkConfiguration>()
                    .Instance(_mockPostmarkConfiguration)
                    .LifestyleSingleton());

            // Create test subject
            _sut = new TestablePostmarkEmailSender(
                _mockConfiguration,
                _mockClientBuilder,
                _mockClientWrapper)
            {
                Logger = _mockLogger
            };
        }

        protected override void PostInitialize()
        {
            // Get configuration from test module
            var configuration = LocalIocManager.Resolve<IAbpStartupConfiguration>();
            configuration.Get<IAbpPostmarkConfiguration>().ApiKey = "test-api-key";
        }

        [Fact]
        public async Task SendEmailAsync_BasicEmail_ShouldSendViaPostmark()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "Test Body",
                IsBodyHtml = false
            };

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid() });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.To == "recipient@example.com" &&
                msg.Subject == "Test Subject" &&
                msg.TextBody == "Test Body" &&
                msg.HtmlBody == null
            ));
        }

        [Fact]
        public async Task SendEmailAsync_HtmlEmail_ShouldSendWithHtmlBody()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "<p>Test Body</p>",
                IsBodyHtml = true
            };

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid() });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.HtmlBody == "<p>Test Body</p>" &&
                msg.TextBody == null
            ));
        }

        [Fact]
        public async Task SendEmailAsync_WithTemplate_ShouldSendTemplatedEmail()
        {
            // Arrange
            var templateModel = new { name = "Test User" };
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject"
            }.UseTemplate("welcome-template", templateModel);

            _mockClientWrapper.SendEmailWithTemplateAsync(Arg.Any<TemplatedPostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid() });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendEmailWithTemplateAsync(Arg.Is<TemplatedPostmarkMessage>(msg =>
                msg.To == "recipient@example.com" &&
                msg.TemplateAlias == "welcome-template"
            ));
        }

        [Fact]
        public async Task SendEmailAsync_PostmarkError_ShouldThrowAbpException()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "Test Body"
            };

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.UserError, Message = "Invalid API key" });

            // Act & Assert
            var exception = await Should.ThrowAsync<Abp.AbpException>(() => _sut.SendEmailAsync(mail));
            exception.Message.ShouldContain("Failed to send email via Postmark");
        }

        [Fact]
        public async Task SendEmailAsync_WithCcAndBcc_ShouldIncludeAllRecipients()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("to@example.com") },
                CC = { new MailAddress("cc@example.com") },
                Bcc = { new MailAddress("bcc@example.com") },
                Subject = "Test Subject",
                Body = "Test Body"
            };

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid() });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.To == "to@example.com" &&
                msg.Cc == "cc@example.com" &&
                msg.Bcc == "bcc@example.com"
            ));
        }

        [Fact]
        public async Task SendEmailAsync_WithAttachments_ShouldIncludeAttachments()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "Test Body"
            };

            var attachment = new Attachment(new System.IO.MemoryStream(new byte[] { 1, 2, 3 }), "test.txt", "text/plain");
            mail.Attachments.Add(attachment);

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid()});

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.Attachments != null &&
                msg.Attachments.Count == 1 &&
                msg.Attachments.Any(a =>
                    a.Name == "test.txt" &&
                    a.ContentType == "text/plain"
                )));
        }
    }
}
