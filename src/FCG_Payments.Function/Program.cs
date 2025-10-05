using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;

var host = new HostBuilder()
	.ConfigureFunctionsWorkerDefaults()
	.ConfigureServices(services =>
	{
		services.AddHttpClient();
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();

		services.Configure<JsonSerializerOptions>(options =>
		{
			options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			options.PropertyNameCaseInsensitive = true;
		});

		services.Configure<WorkerOptions>(options =>
		{
			options.Serializer = new JsonObjectSerializer(
				services.BuildServiceProvider().GetRequiredService<IOptions<JsonSerializerOptions>>().Value
				);
		});
	})
	.Build();

host.Run();
