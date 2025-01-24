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

        [Fact]
        public void BuildPostmarkClient_ShouldConfigureClientCorrectly()
        {
            // Test DefaultPostmarkClientBuilder
            var config = Substitute.For<IAbpPostmarkConfiguration>();
            config.ApiKey.Returns("test-key");

            var builder = new DefaultPostmarkClientBuilder(config);
            var client = builder.Build();

            // Verify client was configured with API key
            client.ShouldNotBeNull();
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptyRecipients_ShouldThrowException()
        {
            // Arrange
            var mail = new MailMessage();

            // Act & Assert
            await Should.ThrowAsync<AbpException>(() => _sut.SendEmailAsync(mail));
        }

        [Fact]
        public async Task CreateBasicMessageAsync_WithReplyTo_ShouldSetReplyToAddress()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("test@example.com") },
                Subject = "Test",
                Body = "Test body"
            };
            mail.ReplyToList.Add(new MailAddress("reply@example.com"));

            // Act
            var message = await _sut.CreateBasicMessageForTesting(mail);

            // Assert
            message.ReplyTo.ShouldBe("reply@example.com");
        }

        [Fact]
        public async Task SendEmailAsync_WithCustomHeaders_ShouldIncludeHeaders()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("test@example.com") },
                Subject = "Test",
                Body = "Test body"
            };
            mail.Headers.Add("X-Custom-Header", "CustomValue");

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.Headers != null &&
                msg.Headers.Any(h => h.Name == "X-Custom-Header") &&
                msg.Headers.All(h => h.Value == "CustomValue")
            ));
        }

        // Additional configuration tests for AbpPostmarkConfiguration
        [Fact]
        public void AbpPostmarkConfiguration_DefaultProperties_ShouldBeSet()
        {
            var config = new AbpPostmarkConfiguration();

            config.ApiKey.ShouldBeNull();
            config.DefaultFromAddress.ShouldBeNull();
            config.TrackOpens.ShouldBeNull();
        }

        [Fact]
        public async Task SendEmailAsync_WithTag_ShouldIncludeTagHeader()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("test@example.com") },
                Subject = "Test",
                Body = "Test body"
            }.WithTag("test-tag");

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.Tag == "test-tag"
            ));
        }

        [Fact]
        public async Task SendEmailAsync_WithTrackLinks_Enabled_ShouldSetTrackLinksOption()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("test@example.com") },
                Subject = "Test",
                Body = "Test body"
            }.WithTrackLinks(true);

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.TrackLinks == LinkTrackingOptions.HtmlAndText
            ));
        }

        [Fact]
        public async Task SendEmailAsync_WithTrackLinks_Disabled_ShouldSetNoTracking()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("test@example.com") },
                Subject = "Test",
                Body = "Test body"
            }.WithTrackLinks(false);

            _mockClientWrapper.SendMessageAsync(Arg.Any<PostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendMessageAsync(Arg.Is<PostmarkMessage>(msg =>
                msg.TrackLinks == LinkTrackingOptions.None
            ));
        }

        [Fact]
        public void WithTag_ShouldAddHeader()
        {
            // Arrange
            var mail = new MailMessage();

            // Act
            var result = mail.WithTag("test-tag");

            // Assert
            result.Headers[PostmarkEmailSender.TagHeader].ShouldBe("test-tag");
            result.ShouldBe(mail); // Verifies fluent return
        }

        [Fact]
        public void WithTrackLinks_ShouldAddHeader()
        {
            // Arrange
            var mail = new MailMessage();

            // Act
            var result = mail.WithTrackLinks(true);

            // Assert
            result.Headers[PostmarkEmailSender.TrackLinksHeader].ShouldBe("True");
            result.ShouldBe(mail); // Verifies fluent return
        }

        [Fact]
        public async Task SendEmailAsync_WithTemplateAndTag_ShouldIncludeTagInTemplatedMessage()
        {
            // Arrange
            var mail = new MailMessage
            {
                To = { new MailAddress("test@example.com") },
                Subject = "Test",
                Body = "{}"
            }.WithTag("test-tag");
            mail.Headers.Add("X-Postmark-Template-Id", "12345");

            _mockClientWrapper.SendEmailWithTemplateAsync(Arg.Any<TemplatedPostmarkMessage>())
                .Returns(new PostmarkResponse { Status = PostmarkStatus.Success });

            // Act
            await _sut.SendEmailAsync(mail);

            // Assert
            await _mockClientWrapper.Received(1).SendEmailWithTemplateAsync(Arg.Is<TemplatedPostmarkMessage>(msg =>
                msg.Tag == "test-tag"
            ));
        }
    }
}
