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

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. Before submitting any changes, make sure to read our contribution guidelines.
