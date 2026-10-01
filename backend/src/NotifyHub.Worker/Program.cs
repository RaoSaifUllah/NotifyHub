using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotifyHub.Infrastructure;
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddInfrastructure(builder.Configuration);
// Delivery processing is added after persistence and identity exit gates pass.
await builder.Build().RunAsync();
