using Newtonsoft.Json;
using PcapDotNet.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColorlightPlugin
{
	public partial class StatusForm : Form
	{
		private ColorlightPlugin _plugin;

		public event EventHandler ReloadSettings;

		public StatusForm(ColorlightPlugin plugin)
		{
			_plugin = plugin;
			InitializeComponent();
		}

		void OnReloadSettings() => ReloadSettings.Invoke(this, null);

		private void StatusForm_Load(object sender, EventArgs e)
		{

		}

		public void StatusFormMeasage(object sender, string e)
		{
			listBox1.Items.Add(e);
		}

		public void ReloadStatusBox(object sender, EventArgs e)
		{
			SetStatusBox();
		}

		private void buttonSave_Click(object sender, EventArgs e)
		{
			PluginSettings settings = new PluginSettings(_plugin._showDir);

			if (outputComboBox.SelectedIndex != -1 && outputComboBox.SelectedIndex < _plugin._allDevices.Count)
				settings.EthernetOutput = _plugin._allDevices[outputComboBox.SelectedIndex].Name;
			settings.MatrixName = matrixComboBox.SelectedItem.ToString();

			settings.Save();
			OnReloadSettings();
		}

		private void StatusForm_Shown(object sender, EventArgs e)
		{
			LoadSettings();
		}

		private void LoadSettings()
		{
			matrixComboBox.Items.Clear();
			outputComboBox.Items.Clear();
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
			int indexM = matrixComboBox.FindString(_plugin._selectedMatrix);
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

					if(device.Name == _plugin._selectedOutput)
					{
						index = i;
					}
				}
			}

			if (index != -1)
			{
				outputComboBox.SelectedIndex = index;
			}
			SetStatusBox();
		}

		private void SetStatusBox()
		{
			textBoxStatus.Text = string.Format("Height:{0} Width:{1} Start Channel:{2} Channels:{3}",
			   _plugin._panelHeight, _plugin._panelWidth, _plugin._startChannel,
			   (_plugin._panelHeight * _plugin._panelWidth * 3));
		}
	}
}
