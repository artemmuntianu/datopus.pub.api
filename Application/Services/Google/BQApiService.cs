using System.Net.Http.Headers;
using datopus.Core.Exceptions;

namespace datopus.Application.Services.Google;

class QueryRequest
{
    public required string query { get; set; }
    public bool useLegacySql { get; set; }
};

public class BQService
{
    public async Task<object?> Query(string query, string projectId, string accessToken)
    {
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        var requestPayload = new QueryRequest { query = query, useLegacySql = false };
        var content = JsonContent.Create(requestPayload);

        var response = await httpClient.PostAsJsonAsync(
            $"https://bigquery.googleapis.com/bigquery/v2/projects/{projectId}/queries",
            requestPayload
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadFromJsonAsync<object>();
            throw new BQException((int)response.StatusCode, errorContent);
        }

        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> GetProject(string projectId, string accessToken)
    {
        using var client = new HttpClient();

        var requestUrl = $"https://bigquery.googleapis.com/bigquery/v2/projects/{projectId}";

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadFromJsonAsync<object>();
            throw new BQException((int)response.StatusCode, errorContent);
        }

        return await response.Content.ReadFromJsonAsync<object>();
    }
}
