using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Net;
using System.IO;

namespace MechMK1.SteamWeb
{
    public static class API
	{

		#region Authentification
		/// <summary>
		/// Helper function to complete the login procedure and check the
		/// credentials.
		/// </summary>
		/// <returns>Whether the login was successful or not.</returns>
		private static AuthInfo Login(string accessToken)
		{
			string response = PostToApi("ISteamWebUserPresenceOAuth/Logon/v0001",
				"?access_token=" + accessToken);

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["umqid"] != null)
				{
					AuthInfo info = new AuthInfo();
					info.SteamId = (string)data["steamid"];
					info.UmqId = (string)data["umqid"];
					info.LastKnownMessage = (int)data["message"];
					info.AccessToken = accessToken;

					return info;
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Authenticate with a username and password.
		/// Sends the SteamGuard e-mail if it has been set up.
		/// </summary>
		/// <param name="username">Username</param>
		/// <param name="password">Password</param>
		/// <param name="emailauthcode">SteamGuard code sent by e-mail</param>
		/// <returns>Indication of the authentication status.</returns>
		public static AuthInfo Authenticate(string username, string password, string emailauthcode = "")
		{
			string response = PostToApi("ISteamOAuth2/GetTokenWithCredentials/v0001",
				"client_id=DE45CD61&grant_type=password&username=" + Uri.EscapeDataString(username) + "&password=" + Uri.EscapeDataString(password) + "&x_emailauthcode=" + emailauthcode + "&scope=read_profile%20write_profile%20read_client%20write_client");

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["access_token"] != null)
				{
					string accessToken = (string)data["access_token"];
					AuthInfo info = Login(accessToken);
					if (info != null)
					{
						return info;
					}
				}
				else if (((string)data["x_errorcode"]).Equals("steamguard_code_required"))
					throw new LoginSteamGuardException();
			}
			
			throw new LoginFailedException();
		}

		/// <summary>
		/// Authenticate with an access token previously retrieved with a username
		/// and password (and SteamGuard code).
		/// </summary>
		/// <param name="accessToken">Access token retrieved with credentials</param>
		/// <returns>Indication of the authentication status.</returns>
		public static AuthInfo Authenticate(string accessToken)
		{
			return Login(accessToken);
		}
		#endregion

		
		/// <summary>
		/// Retrieves information about the server.
		/// </summary>
		/// <returns>Returns a structure with the information.</returns>
		public static ServerInfo GetServerInfo()
		{
			string response = GetApiResponse("ISteamWebAPIUtil/GetServerInfo/v0001");

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["servertime"] != null)
				{
					ServerInfo info = new ServerInfo();
					info.ServerTime = GetUnixTimestamp((long)data["servertime"]);
					info.ServerTimestring = (string)data["servertimestring"];
					return info;
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		internal static WebRequest GetDefaultRequest(string path)
		{
			System.Net.ServicePointManager.Expect100Continue = false;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://63.228.223.110/" + path);
			request.Host = "api.steampowered.com:443";
			request.ProtocolVersion = HttpVersion.Version11;
			request.Accept = "*/*";
			request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
			request.Headers[HttpRequestHeader.AcceptLanguage] = "en-us";
			request.UserAgent = "Steam 1291812 / iPhone";

			return request;
		}

		/// <summary>
		/// Helper function to perform Steam API requests.
		/// </summary>
		/// <param name="path">Path URI</param>
		/// <returns>Web response info</returns>
		internal static string GetApiResponse(string path)
		{
			WebRequest request = GetDefaultRequest(path);

			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				if ((int)response.StatusCode != 200) return null;

				string src = new StreamReader(response.GetResponseStream()).ReadToEnd();
				response.Close();
				return src;
			}
			catch (WebException)
			{
				return null;
			}
		}

		private static string PostToApi(string path, string postData)
		{
			WebRequest request = GetDefaultRequest(path);
			try
			{
				request.Method = "POST";
				byte[] postBytes = Encoding.ASCII.GetBytes(postData);
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = postBytes.Length;

				Stream requestStream = request.GetRequestStream();
				requestStream.Write(postBytes, 0, postBytes.Length);
				requestStream.Close();

				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				if ((int)response.StatusCode != 200) return null;

				string src = new StreamReader(response.GetResponseStream()).ReadToEnd();
				response.Close();
				return src;
			}
			catch (WebException)
			{

				return null;
			}

		}

		internal static DateTime GetUnixTimestamp(long timestamp)
		{
			DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return origin.AddSeconds(timestamp);
		}
	}

	#region Enumerations
	/// <summary>
	/// Enumeration of possible authentication results.
	/// </summary>
	public enum LoginStatus
	{
		LoginFailed,
		LoginSuccessful,
		SteamGuard
	}

	/// <summary>
	/// Status of a user.
	/// </summary>
	public enum UserStatus
	{
		Offline = 0,
		Online = 1,
		Busy = 2,
		Away = 3,
		Snooze = 4
	}

	/// <summary>
	/// Visibility of a user's profile.
	/// </summary>
	public enum ProfileVisibility
	{
		Private = 1,
		Public = 3,
		FriendsOnly = 8
	}

	/// <summary>
	/// Available sizes of user avatars.
	/// </summary>
	public enum AvatarSize
	{
		Small,
		Medium,
		Large
	}

	/// <summary>
	/// Available update types.
	/// </summary>
	public enum UpdateType
	{
		UserUpdate,
		Message,
		Emote,
		TypingNotification
	}
	#endregion
}
