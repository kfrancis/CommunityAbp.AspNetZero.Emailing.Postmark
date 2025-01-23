using System.Net.Mail;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

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
