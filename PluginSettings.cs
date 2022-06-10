using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorlightPlugin
{
    public class PluginSettings
    {
        string _showFolder;

        public void SetShowFolder(string showFolder)
        {
            _showFolder = showFolder;
        }

        public List<PanelSettings> Load()
        {
            var path = _showFolder + "//ColorlightPlugin2.xml";
            if (!System.IO.File.Exists(path))
                return new List<PanelSettings>();
            System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(List<PanelSettings>));
            System.IO.StreamReader file = new System.IO.StreamReader(path);
             var setting = (List<PanelSettings>)reader.Deserialize(file);
            file.Close();
            return setting;
        }
        public void Save(List<PanelSettings> settings)
        {
            System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(List<PanelSettings>));
            var path = _showFolder + "//ColorlightPlugin2.xml";
            System.IO.FileStream file = System.IO.File.Create(path);
            writer.Serialize(file, settings);
            file.Close();
        }
    }
}
