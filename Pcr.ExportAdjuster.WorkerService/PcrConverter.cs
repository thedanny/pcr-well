using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pcr.ExportAdjuster.WorkerService
{
	public class PcrConverter
	{
		private readonly PcrSetting _setting;

		public PcrConverter(PcrSetting setting)
		{
			_setting = setting;
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
 
		public void ExportPcrWellFormatToStream(Stream inputStream, StreamWriter output)
		{
			var allWellAddresses = GetAllWellAddresses();
			var addressLookupByPosition = allWellAddresses.ToDictionary(a=>a.Position);
			var data = ReadInput(inputStream,addressLookupByPosition);


			const string emptyRow = ",,,,,,,,";
			output.WriteLine("[Sample Setup],,,,,,,,");
			output.WriteLine("Well,Well Position,Sample Name,Target Name,Task,Reporter,Quencher,Quantity,Comments");
		
			
			var settingNc = _setting.Nc;
			var settingPc = _setting.Pc;

			foreach (var address in allWellAddresses)
			{

				//Is Current Address Negative Control
				if (address.Position == settingNc.Position)
				{
					WriteWell(address, settingNc.Name);
				}
				//is current address Positive Control
				else if (address.Position == settingPc.Position)
				{
					WriteWell(address, settingPc.Name);
				}
				//is there sample for the current address
				else if (data.ContainsKey(address.Position))
				{
					var sample=data[address.Position];
					WriteWell(sample.Address,sample.Barcode);
					data.Remove(address.Position);
				}
				//all items written out ?
				else if (!data.Any())
				{
					break;
				}
				//No sample wells
				else
				{
					//TODO; snd Sample.Empty to the helper method
					output.WriteLine(emptyRow);
				}
				
				
				
			}


			output.Flush();
			
			void WriteWell(WellAddress address, string barcode)
			{
				foreach (var t in _setting.Tests)
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