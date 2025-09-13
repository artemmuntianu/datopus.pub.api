using System.Text.Json.Serialization;
using datopus.Core.Entities.Auth;

namespace datopus.Api.DTOs;

public class UpdateUserPayload
{
    [JsonPropertyName("app_metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public AppMetaData? AppMetaData { get; set; }

    [JsonPropertyName("user_metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public UserMetaData? UserMetaData { get; set; }
}
