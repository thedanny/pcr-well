using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pcr.ExportAdjuster.WorkerService
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		private readonly IConfiguration _configuration;
		private readonly IOptions<PcrSetting> _pcrOptions;
		private FileSystemWatcher _watcher;
		private ConcurrentQueue<string> _queue;
		private PcrConverter _converter;

		public Worker(ILogger<Worker> logger,IConfiguration configuration,IOptions<PcrSetting> pcrOptions)
		{
			_logger = logger;
			_configuration = configuration;
			_pcrOptions = pcrOptions;
			_queue=new ConcurrentQueue<string>();
			_converter=new PcrConverter(_pcrOptions.Value);
			_watcher = new FileSystemWatcher()
			{
				Path = @"C:\Data\Pcr-samples\FileWatcher\",
				Filter = "*.csv",
				NotifyFilter = NotifyFilters.LastAccess
				               | NotifyFilters.LastWrite
				               | NotifyFilters.FileName
				               | NotifyFilters.DirectoryName,
				EnableRaisingEvents = true,
			};
			_watcher.Created += NewFileCreated;

		}

		private void NewFileCreated(object sender, FileSystemEventArgs e)
		{
		  
			_queue.Enqueue(e.FullPath);
			
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				if (_queue.IsEmpty)
				{
					_logger.LogInformation($"No new file found");
					
				}
				else
				{
					
					if (_queue.TryDequeue(out var filePath))
					{
						_logger.LogInformation($"New item found {filePath}");
						await using var source=File.OpenRead(filePath);
						var tmp = filePath+"_converted.csv";
						_logger.LogInformation($"Creating  item found {tmp}");
						await using var sw = new StreamWriter(tmp);
						_converter.ExportPcrWellFormatToStream(source,sw);
						_logger.LogInformation($"Done Creating  item found {tmp}");
					}
				}
			
				await Task.Delay(1000, stoppingToken);
			}
		}
	}
}