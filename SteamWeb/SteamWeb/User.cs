using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	/// <summary>
	/// Structure containing extensive user info.
	/// </summary>
	public class User : SteamProfile
	{
		public ProfileVisibility ProfileVisibility { get; set; }
		public int ProfileState { get; set; }
		public string Nickname { get; set; }
		public DateTime LastLogoff { get; set; }
		public UserStatus Status { get; set; }
		public string RealName { get; set; }
		public string PrimaryGroupId { get; set; }
		public DateTime JoinDate { get; set; }
	}
}
