using System.Text.Json.Serialization;

namespace datopus.Api.DTOs;

public class GoogleOAuthRefreshToken
{
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; set; }
}
