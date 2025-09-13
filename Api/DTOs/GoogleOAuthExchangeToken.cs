using System.Text.Json.Serialization;

namespace datopus.Api.DTOs;

public class GoogleOAuthExchangeToken
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("redirect_uri")]
    public required string RedirectUri { get; set; }
}
