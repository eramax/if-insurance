using Newtonsoft.Json;
using System.Text;
using IntegrationTests.Configuration;

namespace IntegrationTests.Helpers;

public class HttpTestHelper
{
    private readonly HttpClient _httpClient;
    private readonly TestConfiguration _config;

    public HttpTestHelper(TestConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    public async Task<HttpResponseMessage> PostJsonAsync<T>(string baseUrl, string endpoint, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        return await _httpClient.PostAsync(url, content);
    }

    public async Task<HttpResponseMessage> GetAsync(string baseUrl, string endpoint)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        return await _httpClient.GetAsync(url);
    }

    public async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
