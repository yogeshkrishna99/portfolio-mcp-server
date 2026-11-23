using System.Net;
using System.Text;
using PortfolioMcp;
using Xunit;

namespace PortfolioMcp.Tests;

/// <summary>Records each request and returns a canned response, so tests need no running API.</summary>
internal sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}

public class PortfolioApiClientTests
{
    private static (PortfolioApiClient client, FakeHandler handler) Create(HttpStatusCode status, string body)
    {
        var handler = new FakeHandler(status, body);
        var http = new HttpClient(handler);
        PortfolioApiClient.Configure(http, "https://api.test", "test-token");
        return (new PortfolioApiClient(http), handler);
    }

    [Fact]
    public async Task GetPriceAsync_CallsPriceEndpointWithExchangeAndBearerToken()
    {
        var (client, handler) = Create(HttpStatusCode.OK, """{"symbol":"RELIANCE","exchange":"NSE","price":2500.5}""");

        var result = await client.GetPriceAsync("RELIANCE", "NSE");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://api.test/api/stocks/RELIANCE/price?exchange=NSE", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-token", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal(2500.5m, result.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task GetSummaryAsync_ThrowsWithApiMessage_WhenPortfolioNotFound()
    {
        var (client, _) = Create(HttpStatusCode.NotFound, """{"message":"Portfolio 99 not found"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetSummaryAsync(99));

        Assert.Contains("404", ex.Message);
        Assert.Contains("Portfolio 99 not found", ex.Message);
    }

    [Fact]
    public async Task BuyAsync_PostsTradeFieldsAsJson()
    {
        var (client, handler) = Create(HttpStatusCode.OK, """{"id":1,"symbol":"TCS","type":"Buy"}""");

        await client.BuyAsync(portfolioId: 3, symbol: "TCS", exchange: "NSE", quantity: 10m, price: 3800m);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.test/api/portfolio/3/buy", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("\"symbol\":\"TCS\"", handler.LastBody);
        Assert.Contains("\"exchange\":0", handler.LastBody); // NSE is sent as its enum value
        Assert.Contains("\"quantity\":10", handler.LastBody);
        Assert.Contains("\"price\":3800", handler.LastBody);
    }
}
