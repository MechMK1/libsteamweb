using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MechMK1.SteamWeb
{
	public class AuthInfo : SteamIdentifiable
	{
		public string UmqId { get; set; }
		public int Message { get; set; }
		public string AccessToken { get; set; }
	}
}
