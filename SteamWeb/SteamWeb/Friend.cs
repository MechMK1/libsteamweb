using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	/// <summary>
	/// Structure containing basic friend info.
	/// </summary>
	class Friend : SteamIdentifiable
	{
		public bool Blocked { get; set; }
		public DateTime FriendSince { get; set; }
	}
}
