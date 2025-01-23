using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Net.Mail;
using Abp.Runtime.Validation;
using CommunityAbp.AspNetZero.Emailing.Postmark;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoilerplatePostmarkTest.Web.Controllers
{
    public class EmailModel
    {
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;

        public string? Template { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }
    }

    public class HomeController : BoilerplatePostmarkTestControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IEmailSender emailSender, ILogger<HomeController> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index([FromForm] EmailModel model)
        {
            if (!ModelState.IsValid) return View();

            try
            {
                var mail = new MailMessage();
                mail.To.Add(model.EmailAddress);

                if (!string.IsNullOrEmpty(model.Template))
                {
                    if (long.TryParse(model.Template, out var templateId))
                    {
                        mail.UseTemplate(templateId, new
                        {
                            userName = "John Doe",
                            confirmationLink = "https://example.com/confirm/123"
                        });
                    }
                    else
                    {
                        mail.UseTemplate(model.Template, new
                        {
                            userName = "John Doe",
                            confirmationLink = "https://example.com/confirm/123"
                        });
                    }
                }
                else
                {
                    mail.Subject = model.Subject;
                    mail.Body = model.Body;
                }

                await _emailSender.SendAsync(mail);
                TempData["Success"] = "Email sent successfully";
                return RedirectToAction(nameof(Index), model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {EmailAddress}", model.EmailAddress);
                ModelState.AddModelError("", "Failed to send email. Please try again.");
                return View(model);
            }
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
