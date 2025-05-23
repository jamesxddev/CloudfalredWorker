using CloudFlaredWorker;
using CloudFlaredWorker.Model;

var builder = Host.CreateDefaultBuilder(args);

// Enable Windows Service support
builder.UseWindowsService();
builder.ConfigureServices((context, services) =>
{
    services.Configure<CloudflareSettings>(
        context.Configuration.GetSection("Cloudflare"));

    services.AddHostedService<Worker>();
});

var host = builder.Build();
host.Run();
