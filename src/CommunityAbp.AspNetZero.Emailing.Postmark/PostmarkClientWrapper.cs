using System.Threading.Tasks;
using PostmarkDotNet;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

public interface IPostmarkClientWrapper
{
    Task<PostmarkResponse> SendEmailWithTemplateAsync(TemplatedPostmarkMessage message);

    Task<PostmarkResponse> SendMessageAsync(PostmarkMessage message);
}

public class PostmarkClientWrapper : IPostmarkClientWrapper
{
    private readonly PostmarkClient _client;

    public PostmarkClientWrapper(PostmarkClient client)
    {
        _client = client;
    }

    public Task<PostmarkResponse> SendEmailWithTemplateAsync(TemplatedPostmarkMessage message)
    {
        return _client.SendEmailWithTemplateAsync(message);
    }

    public Task<PostmarkResponse> SendMessageAsync(PostmarkMessage message)
    {
        return _client.SendMessageAsync(message);
    }
}
