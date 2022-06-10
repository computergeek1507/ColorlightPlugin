using Newtonsoft.Json;
using PcapDotNet.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColorlightPlugin
{
	public partial class StatusForm : Form
	{
		private ColorlightPlugin _plugin;

		public event EventHandler<PanelSettings> ReloadSettings;

		int testCount = 0;

		public StatusForm(ColorlightPlugin plugin)
		{
			_plugin = plugin;
			InitializeComponent();
		}

		void OnReloadSettings(PanelSettings panel) => ReloadSettings.Invoke(this, panel);

		private void StatusForm_Load(object sender, EventArgs e)
		{

		}

		public void StatusFormMeasage(object sender, string e)
		{
			listBox1.Items.Add(e);
		}

		public void ReloadStatusPanel(object sender, PanelSettings panel)
		{
			LoadSettings(panel);
		}

		public void LoadSelectPanel(object sender, int index)
		{
			LoadPanelList();
			if (index==-1)
			{
				ClearPanel();
				return;
			}
			panelComboBox.SelectedIndex = index;
			//
			//LoadSettings(panel);
		}

		private void buttonSave_Click(object sender, EventArgs e)
		{
			//PluginSettings settings = new PluginSettings(_plugin._showDir);
			string output = string.Empty;
			if (outputComboBox.SelectedIndex != -1 && outputComboBox.SelectedIndex < _plugin._allDevices.Count)
			{
				output = _plugin._allDevices[outputComboBox.SelectedIndex].Name;
			}
			string matrixName = matrixComboBox.SelectedItem.ToString();
			int brightness = decimal.ToInt32(brightnessNumericUpDown.Value);
			_plugin.SetSettings(panelComboBox.SelectedIndex, output, outputComboBox.SelectedIndex, brightness, matrixName);
			//LoadSettings();
		}

		private void StatusForm_Shown(object sender, EventArgs e)
		{
			string strCompTime = Properties.Resources.BuildDate;
			this.Text += " - " + strCompTime;
			LoadPanelList();
			if (panelComboBox.Items.Count > 0)
			{
				panelComboBox.SelectedIndex = 0;
			}
		}

		private void LoadPanelList()
		{
			panelComboBox.Items.Clear();
			panelComboBox.Items.AddRange(_plugin.GetPanelNames());
		}

		private void ClearPanel()
		{
			matrixComboBox.Items.Clear();
			outputComboBox.Items.Clear();
			textBoxStatus.Text = string.Empty;
		}

		private void LoadSettings(PanelSettings panel)
		{
			ClearPanel();

			brightnessNumericUpDown.Value = panel.Brightness;

			string result;
			_plugin.xSchedule_Action("GetMatrices", "", "", out result);

			var product = new { Name = "", Price = 0 };
			MatricesData matrices = JsonConvert.DeserializeObject<MatricesData>(result);

			if (matrices.Matrices.Length == 0)
			{
				listBox1.Items.Add("No Matrices Defined in xSchedule.");
			}

			foreach (var m in matrices.Matrices)
			{
				matrixComboBox.Items.Add(m);
			}
			int indexM = matrixComboBox.FindString(panel.MatrixName);
			if (indexM != -1)
			{
				matrixComboBox.SelectedIndex = indexM;
			}

			int index = -1;
			if (_plugin._allDevices.Count == 0)
			{
				listBox1.Items.Add("No interfaces found! Make sure WinPcap is installed.");
			}
			else
			{
				for (int i = 0; i != _plugin._allDevices.Count; ++i)
				{
					LivePacketDevice device = _plugin._allDevices[i];
					string fullName = device.Name;
					if (device.Description != null)
						fullName += (" (" + device.Description + ")");
					outputComboBox.Items.Add(fullName);

					if(device.Name == panel.OutputName)
					{
						index = i;
					}
				}
			}

			if (index != -1)
			{
				outputComboBox.SelectedIndex = index;
			}
			SetStatusBox(panel);

			
		}

		private void SetStatusBox(PanelSettings panel)
		{
			textBoxStatus.Text = string.Format("Height:{0} Width:{1} Start Channel:{2} Channels:{3}",
			   panel.PanelHeight, panel.PanelWidth, panel.StartChannel,
			   (panel.PanelHeight * panel.PanelWidth * 3));
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (checkBoxTest.Checked)
			{
				var panel = _plugin.GetPanel(panelComboBox.SelectedIndex);
				if (null == panel)
                {
					return;
                }
				if (testCount > 2) testCount = 0;
				_plugin.TestPanel(0xFF, testCount, panel);
				++testCount;				
			}
			else
			{
				string result;
				_plugin.xSchedule_Action("GetPlayingStatus", "", "", out result);
				//listBox1.Items.Add(result.ToString());
				if (result.Contains("\"status\":\"idle\""))
				{
					_plugin.TestAllPanels(0x00, 0);
				}
			}
		}

		private void checkBoxTest_CheckedChanged(object sender, EventArgs e)
		{

		}

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
			listBox1.Items.Clear();
		}

        private void buttonAdd_Click(object sender, EventArgs e)
        {
			string panelName = string.Empty;
			if (InputBox.Show(ref panelName, "New Panel Name", this) == DialogResult.OK) 
			{
				_plugin.AddPanel(panelName);
			}
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
			_plugin.RemovePanel(panelComboBox.SelectedIndex);
		}

        private void comboBoxPanels_SelectedIndexChanged(object sender, EventArgs e)
        {
			var panel = _plugin.GetPanel(panelComboBox.SelectedIndex);
			if (panel != null)
			{
				LoadSettings(panel);
			}
		}
    }
}
