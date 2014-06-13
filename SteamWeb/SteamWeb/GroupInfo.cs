using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	/// <summary>
	/// Structure containing extensive group info.
	/// </summary>
	public class GroupInfo : SteamProfile
	{
		public DateTime CreationDate { get; set; }
		public string Name { get; set; }
		public string Headline { get; set; }
		public string Summary { get; set; }
		public string Abbreviation { get; set; }
		public int FavoriteAppId { get; set; }
		public int Members { get; set; }
		public int UsersOnline { get; set; }
		public int UsersInChat { get; set; }
		public int UsersInGame { get; set; }
		public string Owner { get; set; }
	}
}
