# Portfolio MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) server written in C# that lets an AI assistant read and update a stock portfolio. It wraps the [StockPortfolio](https://github.com/yogeshkrishna99/stock-portfolio) .NET API, so the assistant gets the same permissions as the web app.

Ask an assistant things like:

- "What's the current price of RELIANCE on NSE?"
- "Show my portfolio 3 summary and tell me which positions are down."
- "Record a buy of 10 TCS at 3800 in portfolio 3."

## Tools

| Tool | What it does | Calls |
|---|---|---|
| `get_stock_price` | Latest price for a symbol on NSE, BSE or US | `GET api/stocks/{symbol}/price` |
| `get_portfolio_summary` | Positions, invested value, current value and P&L | `GET api/portfolio/{id}/summary` |
| `record_buy` | Records a buy trade (validates quantity and price first) | `POST api/portfolio/{id}/buy` |

`record_buy` changes account data, so the tool description tells the assistant to confirm the details before calling it.

## Architecture

```
AI assistant ──stdio (MCP)──▶ PortfolioMcp ──HTTPS + JWT──▶ StockPortfolio API ──▶ SQL Server
```

- **`PortfolioApiClient`** is a typed `HttpClient` wrapper. It attaches the bearer token and turns API errors into messages the assistant can show the user.
- **`PortfolioTools`** holds the MCP tool definitions. Each method has a `[Description]` on every parameter, so the model knows what to pass.
- **`Program.cs`** sets up the host with `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`. Logs go to stderr, because stdout carries the protocol.

## Setup

### Prerequisites
- .NET 9 SDK
- The StockPortfolio API running locally, or a deployed URL
- A JWT from `POST /api/auth/login`

### Configure

```bash
export PORTFOLIO_API_URL=https://localhost:7001     # default shown
export PORTFOLIO_API_TOKEN=<jwt from /api/auth/login>
```

### Run and test

```bash
dotnet test
dotnet run --project PortfolioMcp
```

### Connect to an MCP client

Add the server to your client's MCP configuration (for example `claude_desktop_config.json` or a project `.mcp.json`):

```json
{
  "mcpServers": {
    "portfolio": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/portfolio-mcp-server/PortfolioMcp"],
      "env": {
        "PORTFOLIO_API_URL": "https://localhost:7001",
        "PORTFOLIO_API_TOKEN": "<jwt>"
      }
    }
  }
}
```

## Tests

Three unit tests cover `PortfolioApiClient` against a fake `HttpMessageHandler`, so they run without the API:

1. Price request uses the correct route, exchange query and bearer token.
2. An API 404 is surfaced as an error that includes the API's message.
3. A buy request posts the trade fields as JSON.

## Limitations and next steps

- The tools need a token that is already valid. Login and token refresh are not wrapped yet.
- Tests cover the HTTP client, not the MCP tool wiring. The tools were checked by hand against a running API: `record_buy` recorded a trade and `get_portfolio_summary` returned the portfolio.
- `get_portfolio_summary` depends on live prices from Yahoo Finance. When Yahoo rate-limits (HTTP 429), the API returns a 500 for the whole summary instead of a partial result. This is a known issue in the API, not in this server.
- The stock-price source is the Yahoo Finance endpoint, which is unofficial and can change without notice.

## Troubleshooting

- **SSL error with `https://localhost`:** the local development certificate isn't trusted. Run `dotnet dev-certs https --trust` once, or point `PORTFOLIO_API_URL` at an `http://` address.
- **`Portfolio API returned 400` on `record_buy`:** check the exchange is `NSE`, `BSE` or `US`. The server maps these to the API's numeric enum values.
