using Microsoft.Extensions.Logging;

namespace Task7
{
	interface IHandler
	{
		Task<IStatusResponse> GetApplicationStatus(string id);
	}

	class Handler : IHandler
	{
		private readonly IClient _client1;
		private readonly IClient _client2;
		private readonly ILogger<Handler> _logger;

		public Handler(IClient client1, IClient client2, ILogger<Handler> logger)
		{
			_client1 = client1;
			_client2 = client2;
			_logger = logger;
		}

		public async Task<IStatusResponse> GetApplicationStatus(string id)
		{
			int retryCount = 0;
			var cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromSeconds(15));
			DateTime lastRequestTime = DateTime.Now;

			try
			{
				bool retrying = false;
				do
				{
					if (retrying)
						_logger.LogInformation("Getting application status '{Id}', retry {Retry}", id, retryCount);
					else
						_logger.LogInformation("Getting application status '{Id}'", id);

					retrying = false;

					lastRequestTime = DateTime.Now;
					var res1 = _client1.GetApplicationStatus(id, cts.Token);
					var res2 = _client2.GetApplicationStatus(id, cts.Token);

					var res = await Task.WhenAny(res1, res2);

					switch (res.Result)
					{
						case SuccessResponse sr:
							return new SuccessStatus(sr.Id, sr.Status);
						case FailureResponse fr:
							return new FailureStatus(lastRequestTime, retryCount);
						case RetryResponse rr:
							_logger.LogInformation("Retrying in {Period:N3} sec", rr.Delay.TotalSeconds);
							await Task.Delay(rr.Delay, cts.Token);
							retryCount++;
							retrying = true;
							break;
						default:
							throw new InvalidOperationException("Unknown status");
					}

				} while (retrying);
			}
			catch (OperationCanceledException)
			{
				return new FailureStatus(lastRequestTime, retryCount);
			}

			return new FailureStatus(lastRequestTime, retryCount);
		}
	}

	interface IStatusResponse
	{
	}

	record SuccessStatus(string Id, string Status) : IStatusResponse;
	record FailureStatus(DateTime? LastRequestTime, int RetriesCount) : IStatusResponse;
}
