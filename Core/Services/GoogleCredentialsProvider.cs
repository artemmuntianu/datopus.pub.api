using Google.Apis.Auth.OAuth2;

public interface IGoogleCredentialProvider
{
    GoogleCredential GetCredential();
}

public class GoogleCredentialProvider : IGoogleCredentialProvider
{
    private readonly GoogleCredential _credential;

    public GoogleCredentialProvider(IConfiguration configuration)
    {
        var json = configuration["GOOGLE_APPLICATION_CREDENTIALS_JSON"];
        var path = configuration["GOOGLE_APPLICATION_CREDENTIALS"];

        if (!string.IsNullOrEmpty(json))
            _credential = GoogleCredential.FromJson(json);
        else if (!string.IsNullOrEmpty(path))
            _credential = GoogleCredential.FromFile(path);
        else
            throw new InvalidOperationException("No Google credentials found.");
    }

    public GoogleCredential GetCredential() => _credential;
}
