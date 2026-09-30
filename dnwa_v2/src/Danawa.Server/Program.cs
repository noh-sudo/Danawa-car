using System.Text.Json;
using Danawa.Server.Data;
using Danawa.Server.Net;

string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
string settingsJson = await File.ReadAllTextAsync(settingsPath);
using JsonDocument settings = JsonDocument.Parse(settingsJson);
int port = settings.RootElement.GetProperty("Port").GetInt32();
string dbConnectionString = settings.RootElement.GetProperty("DbConnectionString").GetString()!;

Db.Configure(dbConnectionString);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var server = new TcpServer(port);
await server.RunAsync(cts.Token);
