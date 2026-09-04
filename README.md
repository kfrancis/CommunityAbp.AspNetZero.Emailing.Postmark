# CommunityAbp.AspNetZero.Emailing.Postmark

A seamless integration module that enables Postmark email delivery services for [AspNetZero](https://aspnetzero.com/) and [ABP Framework](https://aspnetboilerplate.com/) applications.

## Overview

This package provides a robust implementation of email-sending capabilities using Postmark's API within the AspNetZero and ABP Framework ecosystem. It replaces the default transport (`IEmailSender`, normally MailKit) with Postmark's API while maintaining the simplicity and flexibility of ABP's modular architecture.

## Features

- Drop-in replacement for ABP's default email sender
- Full support for Postmark's transactional email API
- Template management and synchronization
- Email tracking and analytics integration
- Comprehensive logging and monitoring
- Automatic retry handling for failed deliveries
- Support for both synchronous and asynchronous sending
- Batch email processing capabilities

## Framework Support

Each ABP major line ships a single target framework, so the package multi-targets and picks the matching ABP dependency per TFM:

| Your app targets | Package asset used | ABP dependency | Where ABP comes from |
|------------------|--------------------|----------------|----------------------|
| net10.0 (AspNetZero 15.x) | `net10.0` | `Abp >= 11.3.0` | **AspNetZero licensed feed only** (see below) |
| net9.0 (AspNetZero 14.x) | `net9.0` | `Abp >= 10.5.0` | nuget.org |
| net8.0 (AspNetZero 13.x) | `net8.0` | `Abp >= 9.4.2` | nuget.org |
| netstandard2.0 / 2.1 | `netstandard2.x` | `Abp >= 9.4.2` | nuget.org |

> **net10.0 consumers: the AspNetZero feed is required.**
> ABP 11.x packages are not published to nuget.org; they exist only on the licensed AspNetZero NuGet feed
> (`https://nuget.aspnetzero.com/<your-key>/v3/index.json`). Restoring this package's `net10.0` asset
> will fail with `NU1102: Unable to find package Abp with version (>= 11.3.0)` until that feed is added to your solution's
> `NuGet.Config` (AspNetZero 15.x solutions already have it). Older targets are unaffected.

## Installation

```bash
dotnet add package CommunityAbp.AspNetZero.Emailing.Postmark
```

## Quick Start

1. Install the package
2. Add the `Postmark` section to `appsettings.json`
3. Depend on `AbpPostmarkModule` and configure it in `PreInitialize`
4. Inject `IEmailSender` as usual; emails now go through Postmark

## Configuration

```json
{
  "Postmark": {
    "ApiKey": "your-api-key",
    "FromAddress": "sender@yourdomain.com"
  }
}
```

The package does not read `IConfiguration` itself; wire the values up in your module (this is the usual
AspNetZero `*CoreModule` or `*ApplicationModule`):

```csharp
using Abp.Modules;
using CommunityAbp.AspNetZero.Emailing.Postmark;

[DependsOn(typeof(AbpPostmarkModule))]
public class MyProjectCoreModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public MyProjectCoreModule(IWebHostEnvironment env)
    {
        _appConfiguration = AppConfigurations.Get(env.ContentRootPath, env.EnvironmentName);
    }

    public override void PreInitialize()
    {
        Configuration.Modules.AbpPostmark().ApiKey = _appConfiguration["Postmark:ApiKey"];
        Configuration.Modules.AbpPostmark().DefaultFromAddress = _appConfiguration["Postmark:FromAddress"];
        Configuration.Modules.AbpPostmark().TrackOpens = true; // optional
    }
}
```

`AbpPostmarkModule` replaces `IEmailSender` with `PostmarkEmailSender` (transient), so anything that already
injects `IEmailSender` (for example AspNetZero's `UserEmailer`) switches transport without code changes.

### AspNetZero 15.x note

AspNetZero 15 moved email templates into the database (`EmailTemplate` entity + `EmailTemplateProvider`).
This package only replaces the transport, not the templates, so DB-backed templates keep working: the
rendered `MailMessage` is handed to Postmark as-is. Postmark-side templates (`UseTemplate(...)`) remain
available as an alternative.

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
- [x] .NET Standard 2.0 / 2.1 (ABP 9.4.x)
- [x] .NET 8.0 (ABP 9.4.x)
- [x] .NET 9.0 (ABP 10.5.x)
- [x] .NET 10.0 (ABP 11.3.x / AspNetZero 15.x)

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

## Building from source

The repository's `NuGet.Config` references the AspNetZero feed as `%ASPNETZERO_FEED_URL%` so the
per-customer URL is never committed. Set that environment variable before restoring:

```bash
export ASPNETZERO_FEED_URL="https://nuget.aspnetzero.com/<your-key>/v3/index.json"
dotnet build
```

On Windows: `setx ASPNETZERO_FEED_URL "https://nuget.aspnetzero.com/<your-key>/v3/index.json"` (then open a new shell).

CI reads the same value from the `ASPNETZERO_FEED_URL` repository secret. Pull requests from forks do not
receive secrets, so their restore of the `net10.0` target will fail; that is expected.

Versioning uses [MinVer](https://github.com/adamralph/minver) with a `v` tag prefix; pushing a `vX.Y.Z` tag
and publishing a GitHub release triggers the nuget.org push.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. Before submitting any changes, make sure to read our contribution guidelines.
