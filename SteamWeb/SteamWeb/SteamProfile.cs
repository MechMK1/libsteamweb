using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	/// <summary>
	/// Contains basic profile information
	/// </summary>
	public abstract class SteamProfile : SteamIdentifiable
	{
		public string ProfileUrl { get; set; }
		public string AvatarUrl { get; set; }
		public string LocationCountryCode { get; set; }
		public string LocationStateCode { get; set; }
		public int LocationCityId { get; set; }
	
	}
}
