namespace datopus.Core.Entities.Auth;

public class UserClaims
{
    public string? UserId { get; set; }

    public AppMetaData? AppMetaDataClaims { get; set; }

    public UserMetaData? UserMetaDataClaims { get; set; }
}
