namespace Pcr.ExportAdjuster.WorkerService
{
	public  class PcrSetting
	{
		public Control Nc { get; set; }
		public Control Pc { get; set; }
		public Test[] Tests { get; set; }
		
		public string[] Positions { get; set; }
		public string WorkingFolder { get; set; }
		public string ConvertedPath { get; set; }
		public string Prefix { get; set; }
		public bool AppendTimestamp { get; set; }
		
		
		public int RowCount { get; set; }

		public  class Control
		{
			public string Name { get; set; }
			public int Well { get; set; }
			public string Position { get; set; }
		}

		public partial class Test
		{
			public string TargetName { get; set; }
			public string Task { get; set; }
			public string Reporter { get; set; }
			public string Quencher { get; set; }
			public string Quantity { get; set; }
			public string Comments { get; set; }
		}
	}
}