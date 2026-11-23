using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PortfolioMcp;

/// <summary>
/// Thin client for the StockPortfolio REST API. Every call sends the user's JWT
/// as a bearer token, so the API enforces the same permissions as the web app.
/// </summary>
public class PortfolioApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<JsonElement> GetPriceAsync(string symbol, string exchange = "NSE")
    {
        var response = await http.GetAsync($"api/stocks/{Uri.EscapeDataString(symbol)}/price?exchange={exchange}");
        return await ReadAsync(response);
    }

    public async Task<JsonElement> GetSummaryAsync(int portfolioId)
    {
        var response = await http.GetAsync($"api/portfolio/{portfolioId}/summary");
        return await ReadAsync(response);
    }

    public async Task<JsonElement> BuyAsync(int portfolioId, string symbol, string exchange, decimal quantity, decimal price)
    {
        // The API binds the Exchange enum by number (NSE=0, BSE=1, US=2), so map the text to its value.
        var exchangeValue = exchange.ToUpperInvariant() switch
        {
            "NSE" => 0,
            "BSE" => 1,
            "US" => 2,
            _ => throw new ArgumentException($"Unknown exchange '{exchange}'. Use NSE, BSE or US.", nameof(exchange)),
        };
        var body = new { symbol, exchange = exchangeValue, quantity, price, brokerage = 0m };
        var response = await http.PostAsJsonAsync($"api/portfolio/{portfolioId}/buy", body, Json);
        return await ReadAsync(response);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            // Surface the API's own error message so the model can explain it to the user.
            throw new InvalidOperationException($"Portfolio API returned {(int)response.StatusCode}: {content}");
        }
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.Clone();
    }

    /// <summary>Configures the HttpClient base address and bearer token from settings.</summary>
    public static void Configure(HttpClient http, string baseUrl, string token)
    {
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
