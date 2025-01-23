using Abp.Dependency;
using PostmarkDotNet;

namespace CommunityAbp.AspNetZero.Emailing.Postmark;

public interface IPostmarkClientBuilder
{
    IPostmarkClientWrapper Build();
}

public class DefaultPostmarkClientBuilder : IPostmarkClientBuilder, ITransientDependency
{
    private readonly IAbpPostmarkConfiguration _abpPostmarkConfiguration;

    public DefaultPostmarkClientBuilder(IAbpPostmarkConfiguration abpPostmarkConfiguration)
    {
        _abpPostmarkConfiguration = abpPostmarkConfiguration;
    }

    public virtual IPostmarkClientWrapper Build()
    {
        var client = new PostmarkClient(_abpPostmarkConfiguration.ApiKey);
        ConfigureClient(client);
        return new PostmarkClientWrapper(client);
    }

    protected virtual void ConfigureClient(PostmarkClient client)
    {
    }
}
