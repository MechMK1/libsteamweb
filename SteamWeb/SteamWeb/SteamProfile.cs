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
	/// Contains basic profile information
	/// </summary>
	public abstract class SteamProfile : SteamIdentifiable
	{
		public string ProfileUrl { get; set; }
		public string AvatarUrl { get; set; }
		public string LocationCountryCode { get; set; }
		public string LocationStateCode { get; set; }
		public int LocationCityId { get; set; }

		/// <summary>
		/// Retrieve the avatar in the specified format.
		/// </summary>
		/// <param name="size">Requested avatar size</param>
		/// <returns>The avatar as bitmap on success or null on failure.</returns>
		public Bitmap GetAvatar(AvatarSize size = AvatarSize.Small)
		{
			if (this.AvatarUrl.Length == 0) return null;

			try
			{
				WebClient client = new WebClient();

				Stream stream;
				if (size == AvatarSize.Small)
					stream = client.OpenRead(this.AvatarUrl + ".jpg");
				else if (size == AvatarSize.Medium)
					stream = client.OpenRead(this.AvatarUrl + "_medium.jpg");
				else
					stream = client.OpenRead(this.AvatarUrl + "_full.jpg");

				Bitmap avatar = new Bitmap(stream);
				stream.Flush();
				stream.Close();

				return avatar;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
