using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using PortfolioMcp;

// MCP servers talk JSON-RPC over stdout, so every log line must go to stderr.
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

var baseUrl = Environment.GetEnvironmentVariable("PORTFOLIO_API_URL") ?? "https://localhost:7001";
var token = Environment.GetEnvironmentVariable("PORTFOLIO_API_TOKEN")
    ?? throw new InvalidOperationException("Set PORTFOLIO_API_TOKEN to a JWT from POST /api/auth/login.");

builder.Services.AddHttpClient<PortfolioApiClient>(http => PortfolioApiClient.Configure(http, baseUrl, token));

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
