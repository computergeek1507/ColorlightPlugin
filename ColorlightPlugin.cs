using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xScheduleWrapper;
using System.Drawing;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using Newtonsoft.Json;

namespace ColorlightPlugin
{
    public class ColorlightPlugin
	{
		public event EventHandler<string> SendError;
		//public event EventHandler<PanelSettings> SendReloadPanel;
		public event EventHandler<int> SendIndex;

		StatusForm _form;

		List<PanelSettings> _panelList = new List<PanelSettings>();

		bool _outputing = false;

		public IList<LivePacketDevice> _allDevices;

		public string _showDir = "";

		PluginSettings _setting = new PluginSettings();

		void OnSendError(string errorString) => SendError.Invoke(this, errorString);

		//void OnReloadPanel(PanelSettings panel) => SendReloadPanel.Invoke(this, panel);

		void OnSendSetPanel(int index) => SendIndex.Invoke(this, index);

		public string GetMenuString()
		{
			return "Colorlight Plugin";
		}
		public string GetWebFolder()
		{
			return "";
		}

		public bool xSchedule_Action(string command, string parameters, string data, out string buffer)
		{
			return xScheduleWrapper.xScheduleWrapper.Do_xSchedule_Action(command, parameters, data, out buffer);
		}

		public bool Load(string showDir)
		{
			_showDir = showDir;
			_setting.SetShowFolder(_showDir);
			return true;
		}

		public void Unload()
		{
		}

		public bool HandleWeb(string command, string parameters, string data, string reference, out string response)
		{
			response = "";
			return false;
		}

		public bool Start(string showDir)
		{
			_showDir = showDir;
			_setting.SetShowFolder(_showDir);

			if (_form != null) return true;

			_form = new StatusForm(this);

			SendError += _form.StatusFormMeasage;
			//SendReloadPanel += _form.ReloadStatusPanel;
			SendIndex += _form.LoadSelectPanel;
			//_form.ReloadSettings += Reload_Setting;

			_allDevices = LivePacketDevice.AllLocalMachine;
			ReadSetting();

			_form.Show();

			return true;
		}

		public void Stop()
		{
			if (_form == null) return;

			_form.Close();
			_form = null;
		}

		public PanelSettings GetPanel(int index) 
		{
			if (index < _panelList.Count && index >= 0)
			{
				return _panelList[index];
			}
			return null;
		}

		public void WipeSettings()
		{

		}

		public void NotifyStatus(string status)
		{
		}

		/// <summary>
		/// readload settings on event from form window
		/// </summary>
		private void Reload_Setting(object sender, EventArgs e)
		{
			ReadSetting();
		}

		/// <summary>
		/// read the setting XML file from the show directory
		/// </summary>
		private bool ReadSetting()
		{
			_panelList = _setting.Load();

			bool ok = true;

			foreach (var panel in _panelList)
            {
                for (int i = 0; i != _allDevices.Count; ++i)
                {
                    LivePacketDevice device = _allDevices[i];
                    if (device.Name == panel.OutputName)
                    {
                        panel.OutputIndex = i;
                        break;
                    }
                }

                if (panel.OutputIndex == -1)
                {
                    OnSendError("Ethernet Ouput not found");
                    ok = false;
                }

                ok = GetMatixData( panel);
            }
            return ok;
		}

        private bool GetMatixData(PanelSettings panel)
        {
			bool ok = true;

			string result;
            xSchedule_Action("GetMatrix", panel.MatrixName, "", out result);

            Matrix settings = JsonConvert.DeserializeObject<Matrix>(result);
            if (!string.IsNullOrEmpty(settings.name))
            {
                panel.PanelWidth = int.Parse(settings.width);
                panel.PanelHeight = int.Parse(settings.height);
                panel.StartChannel = int.Parse(settings.startchannel);
            }
            else
            {
                OnSendError(panel.MatrixName + " Matrix not found");
                ok = false;
            }

            return ok;
        }

        public void SetSettings(int index, string outputName, int outputIndex,
			int brightness, string matrixName)
		{
			_panelList[index].OutputName = outputName;
			_panelList[index].OutputIndex = outputIndex;
			_panelList[index].Brightness = brightness;
			_panelList[index].MatrixName = matrixName;
			//_panelList[index].PanelHeight = panelHeight;
			//_panelList[index].PanelWidth = panelWidth;
			//_panelList[index].StartChannel = startChannel;

			GetMatixData(_panelList[index]);


			SaveSetting();
			OnSendSetPanel(index);
		}

		public void SaveSetting()
		{
			_setting.Save(_panelList);			
		}

		public void AddPanel(string name)
		{
			PanelSettings panel = new PanelSettings();
			panel.PanelName = name;
			_panelList.Add(panel);
			SaveSetting();
			OnSendSetPanel(_panelList.Count-1);
		}

		public void RemovePanel(int index)
		{
			if(index < _panelList.Count && index >= 0)
			{ 
				_panelList.RemoveAt(index);
				SaveSetting();
				if (_panelList.Count == 0)
				{
					OnSendSetPanel(-1);
				}
				else 
				{
					OnSendSetPanel(0); 
				}
			}			
		}

		public string[] GetPanelNames()
		{
			return _panelList.Select(s=>s.PanelName).ToArray();
		}

		/// <summary>
		/// This function is called for each frame
		/// </summary>
		public void ManipulateBuffer(PixelBuffer buffer)
		{
			OutputToPanel(buffer);
		}

		public void FireEvent(string type, string parameters)
		{
			//MessageBox.Show(parameters);
		}

		/// <summary>
		/// This Outputs the PixelBuffer data to the panel.
		/// </summary>
		async Task OutputToPanel(PixelBuffer buffer)
		{
			if (_outputing)
				return;

			_outputing = true;
			foreach (var panel in _panelList) 
			{
				try
				{
					if (panel.OutputIndex == -1)
					{
						OnSendError(panel.PanelName + " No Ethernet Output Setup, Skipping Output");
						return;
					}

					if (panel.StartChannel == -1)
					{
						OnSendError(panel.PanelName + " No Matrix Set, Skipping Output");
						return;
					}
					PacketDevice selectedDevice = _allDevices[panel.OutputIndex];
					using (PacketCommunicator communicator = selectedDevice.Open(100, // name of the device
																			 PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
																			 100)) // read timeout
					{
						MacAddress source = new MacAddress("22:22:33:44:55:66");

						// set mac destination to 02:02:02:02:02:02
						MacAddress destination = new MacAddress("11:22:33:44:55:66");

						// Ethernet Layer
						int pixelWidth = panel.PanelWidth;
						int pixelHeight = panel.PanelHeight;
						int startChannel = panel.StartChannel;

						communicator.SendPacket(BuildFirstPacket(source, destination));
						communicator.SendPacket(BuildSecondPacket(source, destination));
						for (int i = 0; i < pixelHeight; i++)
						{
							int offset = pixelWidth * i;
							communicator.SendPacket(BuildPixelPacket(source, destination, i, pixelWidth, buffer, startChannel, offset, panel.Brightness));
						}
					}
				}
				catch (Exception ex)
				{
					OnSendError(panel.PanelName + " Error " + ex.Message);
				}
			}			
			_outputing = false;
		}

		/// <summary>
		/// This function builds the Ethernet 0x0101 Packet.
		/// </summary>
		private Packet BuildFirstPacket(MacAddress source, MacAddress destination)
		{
			EthernetLayer ethernetLayer = new EthernetLayer
			{
				Source = source,
				Destination = destination,
				EtherType = ((EthernetType)0x0101)
			};

			PayloadLayer payloadLayer =
				new PayloadLayer
				{
					Data = new Datagram(new byte[98])
				};

			PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

			return builder.Build(DateTime.Now);
		}

		/// <summary>
		/// This function builds the Ethernet 0x0AFF Packet.
		/// </summary>
		private Packet BuildSecondPacket(MacAddress source, MacAddress destination)
		{
			EthernetLayer ethernetLayer = new EthernetLayer
			{
				Source = source,
				Destination = destination,
				EtherType = ((EthernetType)0x0AFF)
			};

			byte[] mainByte = new byte[63];

			mainByte[0] = 0xFF;
			mainByte[1] = 0xFF;
			mainByte[2] = 0xFF;

			PayloadLayer payloadLayer =
				new PayloadLayer
				{
					Data = new Datagram(mainByte)
				};

			PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

			return builder.Build(DateTime.Now);
		}

		/// <summary>
		/// This function builds the Ethernet Row Data Packet.
		/// </summary>
		private Packet BuildPixelPacket(MacAddress source, MacAddress destination, int row, int pixelsWidth, PixelBuffer data, int startChannel, int dataOffset, int brightness)
		{
			int offset = 0;
			int width = pixelsWidth * 3;

			byte[] mainByte = new byte[(width) + 7];

			EthernetType type = ((EthernetType)0x5500);

			if (row < 256)
			{
				type = ((EthernetType)0x5500);
				mainByte[0] = Convert.ToByte(row);
			}
			else
			{
				type = ((EthernetType)0x5501);
				mainByte[0] = Convert.ToByte(row % 256);
			}

			EthernetLayer ethernetLayer = new EthernetLayer
			{
				Source = source,
				Destination = destination,
				EtherType = type
			};

			//mainByte[0] = Convert.ToByte(row);
			mainByte[1] = Convert.ToByte(offset >> 8);
			mainByte[2] = Convert.ToByte(offset & 0xFF);
			mainByte[3] = Convert.ToByte(pixelsWidth >> 8);
			mainByte[4] = Convert.ToByte(pixelsWidth & 0xFF);
			mainByte[5] = 0x08;
			mainByte[6] = 0x80;

			for (int i = 0; i < width; i++)
			{
				int indexwHead = 7 + i;
				byte oldValue = data[i + (dataOffset * 3) + (startChannel - 1)];
				//int oldint = Convert.ToInt32(data[i + (fullDataOffset * 3)]);
				int newint = ((oldValue * brightness) / 100);
				byte newValue = Convert.ToByte(newint);
				mainByte[indexwHead] = newValue;
				//mainByte[indexwHead] = 0x88;
			}

			PayloadLayer payloadLayer =
				new PayloadLayer
				{
					Data = new Datagram(mainByte)
				};

			PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

			return builder.Build(DateTime.Now);
		}

		/// <summary>
		/// This function builds the Ethernet Row Data Packet with every third channel set to a color.
		/// </summary>
		private Packet BuildTestPacket(MacAddress source, MacAddress destination, int row, int pixelsWidth, int dataOffset, byte color, int testOffset, int brigtness)
		{
			int offset = 0;
			int width = pixelsWidth * 3;

			byte[] mainByte = new byte[(width) + 7];

			EthernetType type = ((EthernetType)0x5500);

			if (row < 256)
			{
				type = ((EthernetType)0x5500);
				mainByte[0] = Convert.ToByte(row);
			}
			else
			{
				type = ((EthernetType)0x5501);
				mainByte[0] = Convert.ToByte(row % 256);
			}

			EthernetLayer ethernetLayer = new EthernetLayer
			{
				Source = source,
				Destination = destination,
				EtherType = type
			};

			//mainByte[0] = Convert.ToByte(row);
			mainByte[1] = Convert.ToByte(offset >> 8);
			mainByte[2] = Convert.ToByte(offset & 0xFF);
			mainByte[3] = Convert.ToByte(pixelsWidth >> 8);
			mainByte[4] = Convert.ToByte(pixelsWidth & 0xFF);
			mainByte[5] = 0x08;
			mainByte[6] = 0x80;

			for (int i = 0; i < width; i++)
			{
				int indexwHead = 7 + i;
				byte oldValue = 0;
				if( i % 3 == testOffset)
					oldValue = color;
				//int oldint = Convert.ToInt32(data[i + (fullDataOffset * 3)]);
				int newint = ((oldValue * brigtness) / 100);
				byte newValue = Convert.ToByte(newint);
				mainByte[indexwHead] = newValue;
				//mainByte[indexwHead] = 0x88;
			}

			PayloadLayer payloadLayer =
				new PayloadLayer
				{
					Data = new Datagram(mainByte)
				};

			PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

			return builder.Build(DateTime.Now);
		}

		/// <summary>
		/// This function sets the panel to a color.
		/// </summary>
		public void TestPanel(byte color, int testOffset, PanelSettings panel)
		{
			try
			{
				if (panel.OutputIndex == -1)
				{
					OnSendError(panel.PanelName + " No Ethernet Output Setup, Skipping Output");
					return;
				}

				if (panel.StartChannel == -1)
				{
					OnSendError(panel.PanelName + " No Matrix Set, Skipping Output");
					return;
				}
				PacketDevice selectedDevice = _allDevices[panel.OutputIndex];
				using (PacketCommunicator communicator = selectedDevice.Open(100, // name of the device
																		 PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
																		 100)) // read timeout
				{
					MacAddress source = new MacAddress("22:22:33:44:55:66");

					// set mac destination to 02:02:02:02:02:02
					MacAddress destination = new MacAddress("11:22:33:44:55:66");

					// Ethernet Layer
					int pixelWidth = panel.PanelWidth;
					int pixelHeight = panel.PanelHeight;
					int startChannel = panel.StartChannel;

					communicator.SendPacket(BuildFirstPacket(source, destination));
					communicator.SendPacket(BuildSecondPacket(source, destination));
					for (int i = 0; i < pixelHeight; i++)
					{
						int offset = pixelWidth * i;
						communicator.SendPacket(BuildTestPacket(source, destination, i, pixelWidth, offset, color, testOffset, panel.Brightness));
					}
				}
			}
			catch (Exception ex)
			{
				OnSendError(panel.PanelName + " Error " + ex.Message);
			}
		}

		/// <summary>
		/// This function sets the all panel to a color.
		/// </summary>
		public void TestAllPanels(byte color, int testOffset)
		{
			foreach (var panel in _panelList)
			{
				TestPanel(color, testOffset, panel);
			}
		}

	}
}