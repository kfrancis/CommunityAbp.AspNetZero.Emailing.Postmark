# CommunityAbp.AspNetZero.Emailing.Postmark

A seamless integration module that enables Postmark email delivery services for [AspNetZero](https://aspnetzero.com/) and [ABP Framework](https://aspnetboilerplate.com/) applications.

## Overview

This package provides a robust implementation of email-sending capabilities using Postmark's API within the AspNetZero and ABP Framework ecosystem. It extends the default emailing system (MailKit) with Postmark's powerful features while maintaining the simplicity and flexibility of ABP's modular architecture.

## Features

- Drop-in replacement for ABP's default email sender
- Full support for Postmark's transactional email API
- Template management and synchronization
- Email tracking and analytics integration
- Comprehensive logging and monitoring
- Automatic retry handling for failed deliveries
- Support for both synchronous and asynchronous sending
- Batch email processing capabilities

## Installation

```bash
dotnet add package CommunityAbp.AspNetZero.Emailing.Postmark
```

## Quick Start

1. Install the package
2. Configure your Postmark API credentials in appsettings.json
3. Register the module in your application
4. Start sending emails using Postmark's infrastructure

## Configuration

```json
{
  "Postmark": {
    "ApiKey": "your-api-key",
    "FromAddress": "sender@yourdomain.com"
  }
}
```

## Features

### Email Sending
- [x] Basic email sending support
- [x] HTML and plain text email bodies
- [x] Multiple recipients (To, CC, BCC)
- [x] Custom From address support
- [x] Default From address fallback from configuration
- [x] Custom headers 

### Postmark Feature Support
- [x] Postmark template integration
- [x] Template ID support (numeric identifier)
- [x] Template alias support (string identifier)
- [x] Dynamic template model binding
- [x] JSON serialization of template models
- [x] Email open tracking
- [x] Click tracking
- [x] Tag support

### Attachments
- [x] File attachment support
- [x] Multiple attachments per email
- [x] Content-type detection
- [x] Base64 encoding handling
- [x] Stream position handling

### Configuration & Setup
- [x] Easy module integration with AbpModule
- [x] Automatic dependency injection setup
- [x] Configurable API key
- [x] Configurable sender email
- [x] Custom PostmarkClient configuration support

### Logging & Diagnostics
- [x] Structured logging throughout
- [x] Debug level operational logs
- [x] Information level success logs
- [x] Error level failure logs
- [x] Attachment processing logs
- [x] Template usage logs
- [x] Message ID tracking

### Framework Support
- [x] .NET Standard 2.0 support
- [x] .NET Standard 2.1 support
- [x] .NET 8.0 support

### Developer Experience
- [x] Fluent API for template usage
- [x] Extension methods for common operations
- [x] Clear exception messages
- [x] Consistent with ABP patterns
- [x] Minimal configuration required

## Not Yet Implemented
- [ ] Batch email sending
- [ ] Bounce handling
- [ ] Webhook support
- [ ] Message stream support
- [ ] Server-level configuration
- [ ] Retry policies

## Postmark Templates 

### Template Identification
- Postmark supports two ways to identify templates:
    - `TemplateId`: A numeric identifier (e.g., 1234567)
    - `TemplateAlias`: A string identifier (e.g., "welcome-email")
- These are stored in the mail headers using custom X-headers:

```csharp
mail.Headers["X-Postmark-Template-Id"] = "1234567";
// OR
mail.Headers["X-Postmark-Template-Alias"] = "welcome-email";
```

### Template Model

- The template model contains the variables that will be merged into your template
- It's stored as JSON in the mail body
- Example template model:
```json
{
  "userName": "John Doe",
  "confirmationLink": "https://example.com/confirm/123",
  "expiryDate": "2024-02-01"
}
```

### Full Template Example

```csharp
// Example 1: Using Template ID
var mail = new MailMessage();
mail.To.Add("user@example.com");
mail.UseTemplate(1234567, new { 
    userName = "John Doe",
    confirmationLink = "https://example.com/confirm/123"
});

// Example 2: Using Template Alias
var mail = new MailMessage();
mail.To.Add("user@example.com");
mail.UseTemplate("welcome-email", new { 
    userName = "John Doe",
    confirmationLink = "https://example.com/confirm/123"
});
```

## Attachments

Here's an example of how to send attachments:

```csharp
var mail = new MailMessage();
mail.To.Add("recipient@example.com");
mail.Subject = "Test with attachment";

// Adding a file attachment
mail.Attachments.Add(new Attachment("document.pdf", "application/pdf"));

// For templated emails with attachments
mail.UseTemplate("welcome-email", new { UserName = "John" });

await _emailSender.SendEmailAsync(mail);
```


## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. Before submitting any changes, make sure to read our contribution guidelines.
