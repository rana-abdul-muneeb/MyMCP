using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using MyMCP;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("MCP Server is starting");

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

var tools =builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var sqlTool = new SqlTool();
await builder
    .Build()
    .RunAsync();

//await builder.Build().RunApplicationandPrintConsoleMessage("MCP Server has started. Refresh MCP tool in your Agent.");