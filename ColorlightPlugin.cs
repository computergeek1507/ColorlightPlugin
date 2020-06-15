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
		public event EventHandler SendReloadDimensions;

		public string _showDir = "";
		string _xScheduleURL = "";
		public int _brightness = 100;
		StatusForm _form;

		public IList<LivePacketDevice> _allDevices;
		public int _panelWidth = 0;
		public int _panelHeight = 0;
		public int _startChannel = -1;
		public int _intSelectOutput = -1;
		public string _selectedOutput;
		public string _selectedMatrix;

		void OnSendError(string errorString) => SendError.Invoke(this, errorString);

		void OnReloadDimensions() => SendReloadDimensions.Invoke(this, null);

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

		public bool Start(string showDir, string xScheduleURL)
		{
			_showDir = showDir;
			_xScheduleURL = xScheduleURL;

			if (_form != null) return true;

			_form = new StatusForm(this);

			SendError += _form.StatusFormMeasage;
			SendReloadDimensions += _form.ReloadStatusBox;
			_form.ReloadSettings += Reload_Setting;

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

		public void WipeSettings()
		{

		}

		public void NotifyStatus(string status)
		{
		}

		private void Reload_Setting(object sender, EventArgs e)
		{
			ReadSetting();
		}

		private bool ReadSetting()
		{
			PluginSettings setting = new PluginSettings(_showDir);
			_selectedOutput = setting.EthernetOutput;
			_selectedMatrix = setting.MatrixName;
			_brightness = setting.Brightness;

			for (int i = 0; i != _allDevices.Count; ++i)
			{
				LivePacketDevice device = _allDevices[i];
				if (device.Name == _selectedOutput)
				{
					_intSelectOutput = i;
					break;
				}
			}

			if (_intSelectOutput == -1)
			{
				OnSendError("Ethernet Ouput not found");
				return false;
			}

			string result;
			xSchedule_Action("GetMatrix", _selectedMatrix, "", out result);

			Matrix settings = JsonConvert.DeserializeObject<Matrix>(result);
			if (!string.IsNullOrEmpty(settings.name))
			{
				_panelWidth = int.Parse(settings.width);
				_panelHeight = int.Parse(settings.height);
				_startChannel = int.Parse(settings.startchannel);
				OnReloadDimensions();
			}
			else
			{
				OnSendError(_selectedMatrix + " Matrix not found");
				return false;
			}

			return true;
		}

		public void ManipulateBuffer(PixelBuffer buffer)
		{
			OutputToPanel(buffer);
		}

		public void FireEvent(string type, string parameters)
		{
			//MessageBox.Show(parameters);
		}

		void OutputToPanel(PixelBuffer buffer)
		{
			try
			{
				if (_intSelectOutput == -1)
				{
					OnSendError("No Ethernet Output Setup, Skipping Output");
					return;
				}

				if (_startChannel == -1)
				{
					OnSendError("No Matrix Set, Skipping Output");
					return;
				}
				PacketDevice selectedDevice = _allDevices[_intSelectOutput];
				using (PacketCommunicator communicator = selectedDevice.Open(100, // name of the device
																		 PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
																		 100)) // read timeout
				{
					MacAddress source = new MacAddress("22:22:33:44:55:66");

					// set mac destination to 02:02:02:02:02:02
					MacAddress destination = new MacAddress("11:22:33:44:55:66");

					// Ethernet Layer
					int pixelWidth = _panelWidth;
					int pixelHeight = _panelHeight;
					int startChannel = _startChannel;

					communicator.SendPacket(BuildFirstPacket(source, destination));
					communicator.SendPacket(BuildSecondPacket(source, destination));
					for (int i = 0; i < pixelHeight; i++)
					{
						int offset = pixelWidth * i;
						communicator.SendPacket(BuildPixelPacket(source, destination, i, pixelWidth, buffer, startChannel, offset));
					}
				}
			}
			catch (Exception ex)
			{
				OnSendError(ex.Message);
			}
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
		private Packet BuildPixelPacket(MacAddress source, MacAddress destination, int row, int pixelsWidth, PixelBuffer data, int startChannel, int dataOffset)
		{
			int offset = 0;
			int width = pixelsWidth * 3;

			int fullDataOffset = startChannel + dataOffset;

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
				byte oldValue = data[i + (fullDataOffset * 3)];
				int oldint = Convert.ToInt32(data[i + (fullDataOffset * 3)]);
				int newint = ((oldValue * _brightness) / 100);
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
	}
}