using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	/// <summary>
	/// Common baseclass for every identifiable object
	/// </summary>
	public abstract class SteamIdentifiable
	{
		public string SteamId { get; set; }
	}
}
