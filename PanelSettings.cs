namespace ColorlightPlugin
{
    public class PanelSettings
	{
		public string PanelName { get; set; }
		public int PanelWidth { get; set; } = 0;
		public int PanelHeight { get; set; } = 0;
		public int StartChannel { get; set; } = -1;
		public int Brightness { get; set; } = 100;
		public int OutputIndex { get; set; } = -1;
		public string OutputName { get; set; }
		public string MatrixName { get; set; }
	}
}