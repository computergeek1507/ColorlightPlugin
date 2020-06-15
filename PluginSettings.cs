using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorlightPlugin
{
    public class PluginSettings
    {
        public class Settings
        {
            public string EthernetOutput { get; set; }
            public string MatrixName { get; set; }
        }

        Settings _appSetting = new Settings();
        string _showFolder;

        public PluginSettings(string showFolder)
        {
            _showFolder = showFolder;
            Load();
        }

        public void Load()
        {
            var path = _showFolder + "//ColorlightPlugin.xml";
            if (!System.IO.File.Exists(path))
                return;
            System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
            System.IO.StreamReader file = new System.IO.StreamReader(path);
            _appSetting = (Settings)reader.Deserialize(file);
            file.Close();
        }
        public void Save()
        {
            System.Xml.Serialization.XmlSerializer writer =
                new System.Xml.Serialization.XmlSerializer(typeof(Settings));

            var path = _showFolder + "//ColorlightPlugin.xml";
            System.IO.FileStream file = System.IO.File.Create(path);
            writer.Serialize(file, _appSetting);
            file.Close();
        }
        
        public string EthernetOutput 
        { 
            get { return _appSetting.EthernetOutput; }
            set { _appSetting.EthernetOutput = value; }
        }
        public string MatrixName 
        { 
            get { return _appSetting.MatrixName; }
            set { _appSetting.MatrixName = value; }
        }
    }
}
