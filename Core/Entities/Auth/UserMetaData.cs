using System.Text.Json.Serialization;

namespace datopus.Core.Entities.Auth;

public class UserMetaData
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool? EmailVerified { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("orgName")]
    public string? OrgName { get; set; }

    [JsonPropertyName("orgType")]
    public string? OrgType { get; set; }

    [JsonPropertyName("sub")]
    public string? Sub { get; set; }
}
