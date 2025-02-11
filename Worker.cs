using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;

namespace AE2DataCollector;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly InfluxDBClient _influxDbClient;
    private readonly InfluxDbSettings _settings;
    private readonly Ae2ApiClient _apiClient;

    public Worker(
        ILogger<Worker> logger,
        IOptions<InfluxDbSettings> settings,
        Ae2ApiClient apiClient,
        InfluxDBClient influxDbClient)
    {
        _logger = logger;
        _settings = settings.Value;
        _influxDbClient = influxDbClient;
        _apiClient = apiClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var writeApi = _influxDbClient.GetWriteApi();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var items = await _apiClient.GetItemsAsync();
                var points = CreatePoints(items);

                writeApi.WritePoints(points, _settings.Bucket, _settings.Org);
                _logger.LogInformation("Written {ItemCount} metrics at {DateTime}",
                    items.Count, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting data: {Message}", ex.Message);
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private static List<PointData> CreatePoints(IEnumerable<Item> items)
    {
        return items.Select(item =>
            PointData.Measurement("item_metrics")
                .Tag("item_id", item.ItemId)
                .Tag("item_name", item.ItemName)
                .Tag("item_hashcode", item.HashCode.ToString())
                .Field("quantity", item.Quantity)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns)
        ).ToList();
    }

    public override void Dispose()
    {
        _influxDbClient.Dispose();
        base.Dispose();
    }

    public class InfluxDbSettings
    {
        public string Url { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Org { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
    }
}