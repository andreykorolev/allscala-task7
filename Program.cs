using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Task7
{
	internal class Program
	{
		private static ServiceProvider CreateServices()
		{
			var sp = new ServiceCollection();
			sp.AddTransient<IClient, Client>();
			sp.AddTransient<IHandler, Handler>();
			sp.AddLogging(builder => builder.AddConsole());

			return sp.BuildServiceProvider();
		}

		static async Task Main(string[] args)
		{
			var services = CreateServices();

			var h = services.GetService<IHandler>();
			var logger = services.GetService<ILogger<Program>>();

			var res = await h.GetApplicationStatus(Guid.NewGuid().ToString("N"));
			if (res is SuccessStatus ss)
				logger.LogInformation("Success {Id}", ss.Id);
			else if (res is FailureStatus fs)
				logger.LogInformation("Failure {Time} {Count}", fs.LastRequestTime, fs.RetriesCount);
		}
	}
}
