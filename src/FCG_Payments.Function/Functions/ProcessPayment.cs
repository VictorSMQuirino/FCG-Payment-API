using FCG_Payments.Domain.DTO;
using FCG_Payments.Domain.Enums;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FCG_Payments.Function.Functions;

public static class ProcessPayment
{
	[Function("ProcessPayment_HttpStart")]
	public static async Task<HttpResponseData> HttpStart(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "process-payment")] HttpRequestData req,
		[DurableClient] DurableTaskClient client,
		FunctionContext executionContext)
	{
		ILogger logger = executionContext.GetLogger("ProcessPayment_HttpStart");

		var purchaseData = await req.ReadFromJsonAsync<PurchaseData>();
		if (purchaseData == null)
		{
			var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
			await badRequestResponse.WriteStringAsync("Please provide purchase data in the request body.");
			return badRequestResponse;
		}

		string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
			nameof(ProcessPayment), purchaseData);

		logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

		return client.CreateCheckStatusResponse(req, instanceId);
	}

	[Function(nameof(ProcessPayment))]
	public static async Task<OrderStatus> RunOrchestrator(
			  [OrchestrationTrigger] TaskOrchestrationContext context)
	{
		ILogger logger = context.CreateReplaySafeLogger(nameof(ProcessPayment));

		var purchaseData = context.GetInput<PurchaseData>();
		logger.LogInformation("Orchestration started for user {UserId}", purchaseData?.UserId);

		bool paymentSucceeded = await context.CallActivityAsync<bool>(nameof(ProcessPaymentActivity), purchaseData?.PaymentInfo);

		if (paymentSucceeded)
		{
			var gameAccessData = new GrantGameAccessData(purchaseData!.UserId, purchaseData.GameId);
			await context.CallActivityAsync(nameof(GrantGameAccessActivity), gameAccessData);
			logger.LogInformation("Orchestration finished successfully.");
			return OrderStatus.Approved;
		}
		else
		{
			logger.LogWarning("Orchestration finished with failed payment.");
			return OrderStatus.Failed;
		}
	}

	[Function(nameof(ProcessPaymentActivity))]
	public static bool ProcessPaymentActivity([ActivityTrigger] string paymentData, FunctionContext executionContext)
	{
		ILogger logger = executionContext.GetLogger("ProcessPaymentActivity");
		logger.LogInformation("Processing payment...");

		bool paymendSucceeded = new Random().Next(0, 100) > 5;
		return paymendSucceeded;
	}
}
