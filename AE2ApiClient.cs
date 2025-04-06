using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AE2DataCollector;

public class Ae2ApiClient
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public Ae2ApiClient(IConfiguration config)
    {
        _baseUrl = config["AE2_SERVER_HOST"]!;
        var password = config["AE2_SERVER_PASSWORD"]!;
        _client = new HttpClient
        {
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(
                        System.Text.Encoding.ASCII.GetBytes($"a:{password}")))
            }
        };
    }

    public async Task<List<Item>> GetItemsAsync()
    {
        var response = await _client.GetAsync($"{_baseUrl}/items");
        response.EnsureSuccessStatusCode();
    
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // Add this
        };
    
        var result = await JsonSerializer.DeserializeAsync<ApiResponse<List<Item>>>(
            await response.Content.ReadAsStreamAsync(), 
            options // Add this parameter
        );
    
        return result?.Data ?? throw new Exception("Invalid API response");
    }
    
}

public record Item(
    [property: JsonPropertyName("itemid")] string ItemId,
    [property: JsonPropertyName("itemname")] string ItemName,
    [property: JsonPropertyName("quantity")] double Quantity,
    [property: JsonPropertyName("craftable")] bool Craftable,
    [property: JsonPropertyName("hashcode")] int HashCode);

public class ApiResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;
    
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;
}