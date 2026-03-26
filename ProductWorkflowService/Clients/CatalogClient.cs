using System.Net.Http;
using System.Net.Http.Headers;

public class CatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CatalogClient(HttpClient httpClient, IHttpContextAccessor accessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = accessor;
    }

    private string GetJwtToken()
    {
        var token = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"]
            .ToString();

        if (string.IsNullOrEmpty(token))
            throw new Exception("Authorization token is missing");

        return token;
    }

    private async Task SendPostRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Attach token per request (best practice)
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(GetJwtToken());

        var response = await _httpClient.SendAsync(request);

        // fail fast if something is wrong
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Catalog API Error: {response.StatusCode}, {error}");
        }
    }

    // ---------------- Workflow APIs ----------------

    public async Task SubmitProduct(int productId)
    {
        await SendPostRequest($"/api/products/{productId}/internal/submit");
    }

    public async Task ApproveProduct(int productId)
    {
        await SendPostRequest($"/api/products/{productId}/internal/approve");
    }

    public async Task RejectProduct(int productId)
    {
        await SendPostRequest($"/api/products/{productId}/internal/reject");
    }
}