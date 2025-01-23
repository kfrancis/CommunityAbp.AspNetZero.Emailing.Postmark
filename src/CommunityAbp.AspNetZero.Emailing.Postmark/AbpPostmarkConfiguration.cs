namespace CommunityAbp.AspNetZero.Emailing.Postmark;

/// <summary>
/// Postmark configuration
/// </summary>
public interface IAbpPostmarkConfiguration
{
    public string? ApiKey { get; set; }
    public string? DefaultFromAddress { get; set; }
    public bool? TrackOpens { get; set; }
}

/// <summary>
/// Postmark configuration
/// </summary>
public class AbpPostmarkConfiguration : IAbpPostmarkConfiguration
{
    public string? ApiKey { get; set; }
    public string? DefaultFromAddress { get; set; }
    public bool? TrackOpens { get; set; }
}
