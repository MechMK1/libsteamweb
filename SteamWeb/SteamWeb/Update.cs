using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	/// <summary>
	/// Structure containing information about a single update.
	/// </summary>
	public class Update
	{
		public DateTime Timestamp { get; set; }
		public string Origin { get; set; }
		public bool LocalMessage { get; set; }
		public UpdateType Type { get; set; }
		public string Message { get; set; }
		public UserStatus Status { get; set; }
		public string Nick { get; set; }
	}
}
