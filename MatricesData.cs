using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorlightPlugin
{
	public class MatricesData
	{
		[JsonProperty("Matrices")]
		public string[] Matrices { get; set; }

		[JsonProperty("reference")]
		public string Reference { get; set; }
	}
}
