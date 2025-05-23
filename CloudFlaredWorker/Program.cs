using CloudFlaredWorker;

var builder = Host.CreateDefaultBuilder(args);

// Enable Windows Service support
builder.UseWindowsService();
builder.ConfigureServices(services =>
{
    services.AddHostedService<Worker>();
});

var host = builder.Build();
host.Run();
