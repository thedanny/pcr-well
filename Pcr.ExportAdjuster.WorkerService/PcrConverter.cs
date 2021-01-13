using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Pcr.ExportAdjuster.WorkerService
{
	public class PcrConverter
	{
		private readonly PcrSetting _setting;
		private readonly ILogger<PcrConverter> _logger;

		public PcrConverter(PcrSetting setting, ILogger<PcrConverter> logger)
		{
			_setting = setting;
			_logger = logger;
		}

		private SortedList<string, InputData> ReadInput(Stream input, Dictionary<string, WellAddress> addressLookup)
		{
			var s = new SortedList<string, InputData>();
			var sr = new StreamReader(input);

			var linesRead = 0;
			while (!sr.EndOfStream)
			{
				var data = sr.ReadLine();

				if (linesRead++ == 0) continue;

				if (data == null) continue;

				var parts = data.Split(",");


				var position = parts.ElementAtOrDefault(0) ??
				               throw new ApplicationException($"Well Position is not valid row={linesRead} data={data}");
				var barcode = parts.ElementAtOrDefault(1) ??
				              throw new ApplicationException($"Barcode is not valid row={linesRead} data={data}");
				var _= addressLookup.TryGetValue(position,out var address)
					? address
					: throw new ApplicationException(
						$"Well Position {position} is out of range .number row={linesRead} data={data}");
				

				var inputData = new InputData(address, barcode);
				s.Add(address.Position, inputData);
			}

			return s;
		}

		public WellAddress[] GetAllWellAddresses()
		{
			var rowsPerPos = _setting.RowCount;
			var colLetters = _setting.Positions.Length;
			
			return Enumerable.Range(0, colLetters)
				.Select(letterPos => Enumerable.Range(1, rowsPerPos)
					.Select(number => new WellAddress(rowsPerPos*letterPos+ number, $"{_setting.Positions[letterPos]}{number}")   ).ToArray())
				.SelectMany(_=>_)
				.ToArray();

		}

		public async Task ConvertAsync(string filePath)
		{
			_logger.LogInformation($"Converting File {filePath}");
			var sourceInfo=new FileInfo(filePath);
			var now = DateTime.Now;
			await using var source=File.Open(filePath,FileMode.Open,FileAccess.Read,FileShare.Read);
			
			var dstName = $"{_setting.Prefix}-{now:yyyyMMdd-hhmmtt}_{sourceInfo.Name}";
			var convertedCsv = Path.Combine(_setting.ConvertedPath, dstName);
			
			_logger.LogInformation($"Creating  {convertedCsv}");
			await using var sw = new StreamWriter(File.Open(convertedCsv,FileMode.OpenOrCreate,FileAccess.Write,FileShare.Read));
			ExportPcrWellFormatToStream(source,sw);
			_logger.LogInformation($"Exported {convertedCsv}");
		}
		public void ExportPcrWellFormatToStream(Stream inputStream, StreamWriter output)
		{
			var allWellAddresses = GetAllWellAddresses();
			var addressLookupByPosition = allWellAddresses.ToDictionary(a=>a.Position);
			var data = ReadInput(inputStream,addressLookupByPosition);


			_logger.LogInformation($"Found {data.Count} Rows from source");
			
			output.WriteLine("[Sample Setup],,,,,,,,");
			output.WriteLine("Well,Well Position,Sample Name,Target Name,Task,Reporter,Quencher,Quantity,Comments");
		
			
			

			var controls = _setting.Controls.ToList();

			foreach (var address in allWellAddresses)
			{
				var control = controls.FirstOrDefault(a => a.Position == address.Position);

				//Is Current Address Reserved for Control
				if (control!=null)
				{
					WriteWell(address, control.Name,control.Name);

					//remove written control
					controls.Remove(control);
					
					_logger.LogInformation($"Negative Control Logged at Row {address}");
				}
				//is there sample for the current address
				else if (data.ContainsKey(address.Position))
				{
					var sample=data[address.Position];
					WriteWell(sample.Address,sample.Barcode,WellTypeNames.Sample);
					data.Remove(address.Position);
				}
				//all samples and controls have been written out ?
				else if (!data.Any() && !controls.Any())
				{
					break;
				}
				//Empty wells
				else
				{
					if(_setting.WriteAddressForEmptyWell)
						WriteWell(address,string.Empty,WellTypeNames.EmptyRow);
				}
				
				_logger.LogInformation($"Written {data.Count} Sample Rows Each Containing {_setting.Tests.Length}");

				
				
			}


			output.Flush();
			
			void WriteWell(WellAddress address, string barcode, string wellTypeName)
			{
				foreach (var t in _setting.Tests.Where(a=>a.Type==wellTypeName))
					output.WriteLine($"{address.Well},{address.Position},{barcode}," +
					                 $"{t.TargetName},{t.Task},{t.Reporter}," +
					                 $"{t.Quencher},{t.Quantity},{t.Comments}");
			}
			
			
			
		}


		private class InputData
		{
			public InputData(WellAddress address, string barcode)
			{
				Address = address;
				Barcode = barcode;
				
			}

			public WellAddress Address { get; }
			public string Barcode { get; }
		}

		public class WellAddress
		{
			public WellAddress(int well, string position)
			{
				Well = well;
				Position = position;
			}

			public int Well { get; }
			public string Position { get; }

			public override string ToString()
			{
				return string.Concat(Well.ToString(), " ", Position);
			}
		}

		
	}
}