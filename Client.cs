namespace Task7
{
	public interface IResponse
	{
	}

	record SuccessResponse(string Id, string Status) : IResponse;
	record FailureResponse() : IResponse;
	record RetryResponse(TimeSpan Delay) : IResponse;

	interface IClient
	{
		Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken);
	}

	public class Client : IClient
	{
		static Random _r = new Random();

		public async Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken)
		{
			var num = _r.Next(1, 4);

			if (num == 1)
				return new RetryResponse(TimeSpan.FromSeconds(4));

			await Task.Delay(TimeSpan.FromSeconds(_r.Next(1, 3)));

			if (num == 2)
				return new FailureResponse();

			return new SuccessResponse(id, "OK");
		}
	}
}
