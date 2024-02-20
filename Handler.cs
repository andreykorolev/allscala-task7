using Microsoft.Extensions.Logging;

namespace Task7
{
	interface IHandler
	{
		Task<IStatusResponse> GetApplicationStatus(string id);
	}

	/*
1. Максимальное время работы метода `GetApplicationStatus` не должно превышать 15 секунд (см. п.7).
2. `GetApplicationStatus` должен возвращать ответ клиенту как можно быстрее.
3. В теле `GetApplicationStatus` должны выполняться запросы к сервисам, а также обработка ответов сервисов и преобразование полученных данных в ответ нового метода.
4. В случае получения успешного результата (`SuccessResponse`)/ошибки (`FailureResponse`) хотя бы от одного сервиса необходимо сразу же вернуть его клиенту.
5. В случае получения сообщения о необходимости повторного вызова (`RetryResponse`) - необходимо организовать повторный вызов через указанный интервал времени (`RetryResponse.Delay`).
6. Для успешно выполненной операции вернуть объект `SuccessStatus`, где:
  * `Id` - идентификатор заявки (`SuccessResponse.Id`)
  * `Status` - статус заявки (`SuccessResponse.Status`)
7. В случае возникновения ошибок или таймаута нужно вернуть объект `FailureResponse`, где:
  * `LastRequestTime` - время последнего запроса, завершившегося ошибкой (опциональное);
  * `RetriesCount` - количество запросов к сервисам, которые закончились статусом `RetryResponse`.
*/

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
