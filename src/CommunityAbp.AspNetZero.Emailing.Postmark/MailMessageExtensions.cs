using System;
using System.Net.Mail;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

public static class MailMessageExtensions
{
    public static MailMessage UseTemplate(this MailMessage mail, long templateId, object model)
    {
        mail.Headers.Add(PostmarkEmailSender.TemplateIdHeader, templateId.ToString());
        mail.Body = System.Text.Json.JsonSerializer.Serialize(model);
        return mail;
    }

    public static MailMessage UseTemplate(this MailMessage mail, string templateAlias, object model)
    {
        mail.Headers.Add(PostmarkEmailSender.TemplateAliasHeader, templateAlias);
        mail.Body = System.Text.Json.JsonSerializer.Serialize(model);
        return mail;
    }

    public static MailMessage WithTag(this MailMessage mail, string tag)
    {
        mail.Headers.Add(PostmarkEmailSender.TagHeader, tag);
        return mail;
    }

    public static MailMessage WithTrackLinks(this MailMessage mail, bool shouldTrack)
    {
        mail.Headers.Add(PostmarkEmailSender.TrackLinksHeader, Convert.ToString(shouldTrack));
        return mail;
    }
}
