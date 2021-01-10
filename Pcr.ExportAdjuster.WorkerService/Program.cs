using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Pcr.ExportAdjuster.WorkerService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) 
		{
			var configuration = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.AddCommandLine(args)
				.Build();
			
			return Host.CreateDefaultBuilder(args)
            				.ConfigureServices((hostContext, services) =>
                            {
	                            services.Configure<PcrSetting>(configuration.GetSection("pcrSetting"));
	                            services.AddHostedService<Worker>( );
                            });
		}
			
	}
}