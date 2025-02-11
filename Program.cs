using AE2DataCollector;
using InfluxDB.Client;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<Worker.InfluxDbSettings>(builder.Configuration.GetSection("InfluxDB"));
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<Ae2ApiClient>();
builder.Services.AddSingleton<InfluxDBClient>(sp => 
{
    var settings = sp.GetRequiredService<IOptions<Worker.InfluxDbSettings>>().Value;
    return new InfluxDBClient(settings.Url, settings.Token);
});

var host = builder.Build();
host.Run();
