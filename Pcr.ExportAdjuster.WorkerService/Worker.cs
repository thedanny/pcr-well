using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pcr.ExportAdjuster.WorkerService
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		private readonly PcrSetting _pcrOptions;
		private FileSystemWatcher _watcher;
		private readonly ConcurrentQueue<string> _queue;
		private readonly PcrConverter _converter;

		public Worker(ILogger<Worker> logger,ILogger<PcrConverter> converterLogger,IOptions<PcrSetting> pcrOptions)
		{
			_logger = logger;
			_pcrOptions = pcrOptions.Value;
			_queue=new ConcurrentQueue<string>();
			_converter=new PcrConverter(_pcrOptions,converterLogger);
			
			

		}

		public override Task StartAsync(CancellationToken cancellationToken)
		{
			try
			{
				_logger.LogInformation($"Service Starting");

				if (!Directory.Exists(_pcrOptions.WorkingFolder))
				{
					_logger.LogInformation($"Creating {_pcrOptions.WorkingFolder}");

					Directory.CreateDirectory(_pcrOptions.WorkingFolder);
				
					_logger.LogInformation($"Created {_pcrOptions.WorkingFolder}");

				}

				_watcher = new FileSystemWatcher
				{
					Path = _pcrOptions.WorkingFolder,
					Filter = "*.csv",
					NotifyFilter = NotifyFilters.LastAccess
					               | NotifyFilters.LastWrite
					               | NotifyFilters.FileName
					               | NotifyFilters.DirectoryName,
					EnableRaisingEvents = true,
				};
				_watcher.Created += NewFileCreated;
			
				_logger.LogInformation($"Service Started");

			}
			catch (Exception e)
			{
				_logger.LogError( e.Message+" detail:"+e.ToString());
				throw;
			}
			return base.StartAsync(cancellationToken);
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Service Stopped");
			_watcher.EnableRaisingEvents = false;
			_watcher.Created -= NewFileCreated;
			_watcher.Dispose();
			return base.StopAsync(cancellationToken);
		}

		private void NewFileCreated(object sender, FileSystemEventArgs e)
		{
			if (e.Name.StartsWith(_pcrOptions.Prefix))
			{
				return;
			}
			_logger.LogInformation($"New file Detected {e.Name}");
		    _queue.Enqueue(e.FullPath);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				if (_queue.IsEmpty)
				{
					//_logger.LogInformation($"No new file found");
					
				}
				else
				{
					
					if (_queue.TryDequeue(out var filePath))
					{
						_logger.LogInformation($"Convert {filePath}");
						try
						{
							await _converter.ConvertAsync(filePath);
						}
						catch (Exception e)
						{
							_logger.LogError(e.Message + e.ToString());
						}
						
					}
				}
			
				await Task.Delay(1000, stoppingToken);
			}
		}
	}
}