using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace PortfolioMcp;

/// <summary>MCP tools that let an AI assistant read and update a StockPortfolio account.</summary>
[McpServerToolType]
public class PortfolioTools
{
    [McpServerTool(Name = "get_stock_price"), Description("Get the latest market price for a stock symbol.")]
    public static async Task<string> GetStockPrice(
        PortfolioApiClient api,
        [Description("Ticker symbol, for example RELIANCE or AAPL")] string symbol,
        [Description("Exchange: NSE, BSE or US. Defaults to NSE.")] string exchange = "NSE")
    {
        var result = await api.GetPriceAsync(symbol, exchange);
        return result.GetRawText();
    }

    [McpServerTool(Name = "get_portfolio_summary"), Description("Get a portfolio's positions, invested value, current value and P&L.")]
    public static async Task<string> GetPortfolioSummary(
        PortfolioApiClient api,
        [Description("The numeric portfolio ID")] int portfolioId)
    {
        var result = await api.GetSummaryAsync(portfolioId);
        return result.GetRawText();
    }

    [McpServerTool(Name = "record_buy"), Description("Record a buy trade in a portfolio. Changes the account, so confirm the details with the user first.")]
    public static async Task<string> RecordBuy(
        PortfolioApiClient api,
        [Description("The numeric portfolio ID")] int portfolioId,
        [Description("Ticker symbol")] string symbol,
        [Description("Exchange: NSE, BSE or US")] string exchange,
        [Description("Number of shares, greater than zero")] decimal quantity,
        [Description("Price per share")] decimal price)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (price <= 0) throw new ArgumentException("Price must be greater than zero.", nameof(price));

        var result = await api.BuyAsync(portfolioId, symbol, exchange, quantity, price);
        return result.GetRawText();
    }
}
