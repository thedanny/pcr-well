using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Pcr.ExportAdjuster.WorkerService;

namespace Tests
{
	public class Tests
	{
		private IConfigurationRoot _configuration;
		private IHost _server;


		[SetUp]
		public void Setup()
		{
			_configuration = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();
			
		   _server=	Host.CreateDefaultBuilder(new string[0])
				.ConfigureServices((hostContext, services) =>
				{
					services.AddLogging(loggingBuilder =>
					{
						loggingBuilder.ClearProviders();
						loggingBuilder.AddNLog("NLog.config");
					});
					services.Configure<PcrSetting>(_configuration.GetSection("pcrSetting"));
					
				}).Build();
		}

		[TearDown]
		public async Task Tear()
		{
			await _server.StopAsync();
		}


		[Test]
		public void GetAllWellAddressShouldReturn96Rows()
		{
			var pcrSetting= _server.Services.GetService<IOptions<PcrSetting>>().Value;
			var logger= _server.Services.GetService<ILogger<PcrConverter>>();
			
			var converter=new PcrConverter(pcrSetting,logger);
			var wells= converter.GetAllWellAddresses();

			Console.WriteLine(string.Join("\n",wells.Select(a=>a.ToString())));
			
		}

		[Test]
		public async Task ShouldConvert()
		{
			var logger= _server.Services.GetService<ILogger<PcrConverter>>();
			var pcrSetting= _server.Services.GetService<IOptions<PcrSetting>>();

			var converter=new PcrConverter(pcrSetting.Value,logger);
			var sourcePath = @"C:\Data\Pcr-samples\inputFile.csv";
			await converter.ConvertAsync(sourcePath);

		}
		
		[Test]
		public void ExportPcrWellFormatToStream_shouldExport()
		{
			var pcrSetting= _server.Services.GetService<IOptions<PcrSetting>>();
			
			var sourcePath = @"C:\Data\Pcr-samples\inputFile.csv";
			var logger= _server.Services.GetService<ILogger<PcrConverter>>();

			
			
			using (var source=File.OpenRead(sourcePath))
			{
				var converter=new PcrConverter(pcrSetting.Value,logger);

				var tmp = sourcePath+"_converted.csv";

				using (var sw = new StreamWriter(tmp))
				{
					converter.ExportPcrWellFormatToStream(source,sw);
				}

				Console.WriteLine(File.ReadAllText(tmp));

			}
			Assert.Pass();
		}
	}
}