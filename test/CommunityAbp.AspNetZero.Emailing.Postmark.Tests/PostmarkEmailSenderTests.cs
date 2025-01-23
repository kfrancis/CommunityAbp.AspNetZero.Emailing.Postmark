using System.Net.Mail;
using Abp;
using Abp.Net.Mail;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PostmarkDotNet;
using Shouldly;
using Attachment = System.Net.Mail.Attachment;

namespace CommunityAbp.AspNetZero.Emailing.Postmark.Tests
{
    public class PostmarkEmailSenderTests : AbpPostmarkTestBase
    {
        private readonly IPostmarkClientWrapper _mockClientWrapper;
        private readonly TestablePostmarkEmailSender _sut;

        public PostmarkEmailSenderTests()
        {
            // Mock dependencies
            _mockClientWrapper = Substitute.For<IPostmarkClientWrapper>();
            var mockClientBuilder = Substitute.For<IPostmarkClientBuilder>();
            var mockConfiguration = Substitute.For<IEmailSenderConfiguration>();
            var mockLogger = Substitute.For<ILogger>();
            var mockPostmarkConfiguration = Substitute.For<IAbpPostmarkConfiguration>();

            // Setup configuration
            mockConfiguration.DefaultFromAddress.Returns("test@example.com");
            mockPostmarkConfiguration.ApiKey.Returns("test-api-key");
            mockPostmarkConfiguration.TrackOpens.Returns(true);

            // Create test subject
            _sut = new TestablePostmarkEmailSender(
                mockConfiguration,
                mockPostmarkConfiguration,
                mockClientBuilder,
                _mockClientWrapper)
            {
                Logger = mockLogger
            };
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
        public async Task SendEmailAsync_WithAttachments_ShouldIncludeAttachments()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "Test Body"
            };

            var attachment = new Attachment(new MemoryStream([1, 2, 3]), "test.txt", "text/plain");
            mail.Attachments.Add(attachment);

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid() });

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
        public async Task SendEmailAsync_WithTemplateId_ShouldUseCorrectId()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "{\"user\":\"test\"}"
            };
            mail.Headers.Add("X-Postmark-Template-Id", "12345");

            _mockClientWrapper.SendEmailWithTemplateAsync(Arg.Any<TemplatedPostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid() });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendEmailWithTemplateAsync(Arg.Is<TemplatedPostmarkMessage>(msg =>
                msg.TemplateId == 12345 &&
                msg.TemplateModel != null
            ));
        }

        [Fact]
        public async Task SendEmailAsync_WithInvalidTemplateModel_ShouldThrowAbpException()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "invalid json"
            };
            mail.Headers.Add("X-Postmark-Template-Id", "12345");

            // Act & Assert
            var exception = await Should.ThrowAsync<AbpException>(() => _sut.SendEmailAsync(mail));
            exception.Message.ShouldContain("Failed to deserialize template model");
        }

        [Fact]
        public async Task SendEmailAsync_WithAttachmentStreamError_ShouldThrowException()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "Test Body"
            };

            var mockStream = Substitute.For<Stream>();
            mockStream.CopyToAsync(Arg.Any<Stream>()).Throws(new IOException("Stream error"));

            var attachment = new Attachment(mockStream, "test.txt", "text/plain");
            mail.Attachments.Add(attachment);

            // Act & Assert
            await Should.ThrowAsync<IOException>(() => _sut.SendEmailAsync(mail));
        }

        [Fact]
        public async Task SendEmailAsync_TrackOpensSetting_ShouldBeRespected()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("recipient@example.com") },
                Subject = "Test Subject",
                Body = "Test Body"
            };

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success, MessageID = Guid.NewGuid() });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.TrackOpens == true
            ));
        }
    }
}
