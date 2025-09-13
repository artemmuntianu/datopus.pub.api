namespace datopus.Api.DTOs.SupportRequests;

public class SupportRequest
{
    public string? Subject { get; set; }

    public string? Message { get; set; }

    public bool AllowProjectSupport { get; set; } = false;

    public IFormFileCollection? Screenshots { get; set; }
}
