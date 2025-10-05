using FCG_Payments.Domain.DTO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FCG_Payments.Function.Functions;

public class GrantGameAccessActivity
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;

	public GrantGameAccessActivity(IHttpClientFactory httpClientFactory, IConfiguration configuration)
	{
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
	}

	[Function(nameof(GrantGameAccessActivity))]
	public async Task Run([ActivityTrigger] object dataObj, FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("GrantGameAccessActivity");

		var json = JsonSerializer.Serialize(dataObj);
		logger.LogInformation("Received JSON payload: {Json}", json);

		var serializerOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true
		};

		var data = JsonSerializer.Deserialize<GrantGameAccessData>(JsonSerializer.Serialize(dataObj), serializerOptions);

		logger.LogInformation("Garanting game access for User {UserId} to Game {GameId}", data?.UserId, data?.GameId);

		if (data == null)
		{
			logger.LogError("Activity received null payload after deserialization.");
			throw new InvalidOperationException("GrantGameAccessActivity received invalid input.");
		}

		var gamesApiBaseUrl = _configuration["FCG_Games_API_Url"];
		var httpClient = _httpClientFactory.CreateClient();
		var response = await httpClient.PostAsync($"{gamesApiBaseUrl}/{data.GameId}/guarante-access/{data.UserId}", null);

		if (!response.IsSuccessStatusCode)
		{
			var errorContent = await response.Content.ReadAsStringAsync();
			logger.LogError("Failed to grant game access. Status: {StatusCode}, Response: {ErrorContent}", response.StatusCode, errorContent);
			throw new HttpRequestException($"Failed to call Games microservice. Status code: {response.StatusCode}");
		}

		logger.LogInformation("Successfully granted game access for User {UserId} to Game {GameId}", data.UserId, data.GameId);
	}
}
