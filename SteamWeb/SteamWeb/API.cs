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
		private static int magicalCounterOfUnknownPurpose = 0;

		/// <summary>
		/// Helper function to complete the login procedure and check the
		/// credentials.
		/// </summary>
		/// <returns>Whether the login was successful or not.</returns>
		private static AuthInfo Login(string accessToken)
		{
			string response = PostApiResponse("ISteamWebUserPresenceOAuth/Logon/v0001",
				"?access_token=" + accessToken);

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["umqid"] != null)
				{
					AuthInfo info = new AuthInfo();
					info.SteamId = (string)data["steamid"];
					info.UmqId = (string)data["umqid"];
					info.Message = (int)data["message"];
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
			string response = PostApiResponse("ISteamOAuth2/GetTokenWithCredentials/v0001",
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

		/// <summary>
		/// Fetch all friends of a given user.
		/// </summary>
		/// <remarks>This function does not provide detailed information.</remarks>
		/// <param name="steamId">steamid of target user or self</param>
		/// <returns>List of friends or null on failure.</returns>
		public static List<Friend> GetFriends(AuthInfo info, string steamId)
		{
			if (info.UmqId == null) return null;
			if (steamId == null) return null;

			string response = GetApiResponse("ISteamUserOAuth/GetFriendList/v0001?access_token=" + info.AccessToken + "&steamid=" + steamId);

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["friends"] != null)
				{
					List<Friend> friends = new List<Friend>();

					foreach (JObject friend in data["friends"])
					{
						Friend f = new Friend();
						f.SteamId = (string)friend["steamid"];
						f.Blocked = ((string)friend["relationship"]).Equals("ignored");
						f.FriendSince = GetUnixTimestamp((long)friend["friend_since"]);
						friends.Add(f);
					}

					return friends;
				}
			}
			
			return null;

		}

		/// <summary>
		/// Retrieve information about the specified users.
		/// </summary>
		/// <remarks>This function doesn't have the 100 users limit the original API has.</remarks>
		/// <param name="steamids">64-bit SteamIDs of users</param>
		/// <returns>Information about the specified users</returns>
		public static List<User> GetUserInfo(AuthInfo info, List<string> steamIds)
		{
			if (info.UmqId == null) return null;

			string response = GetApiResponse(
				"ISteamUserOAuth/GetUserSummaries/v0001?access_token=" + info.AccessToken +
				"&steamids=" + string.Join(",", steamIds.GetRange(0, Math.Min(steamIds.Count, 100)).ToArray()));

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["players"] != null)
				{
					List<User> users = new List<User>();

					foreach (JObject player in data["players"])
					{
						User user = new User();

						user.SteamId = (string)player["steamid"];
						user.ProfileVisibility = (ProfileVisibility)(int)player["communityvisibilitystate"];
						user.ProfileState = (int)player["profilestate"];
						user.Nickname = (string)player["personaname"];
						user.LastLogoff = GetUnixTimestamp((long)player["lastlogoff"]);
						user.ProfileUrl = (string)player["profileurl"];
						user.Status = (UserStatus)(int)player["personastate"];

						user.AvatarUrl = player["avatar"] != null ? (string)player["avatar"] : "";
						if (user.AvatarUrl != null) user.AvatarUrl = user.AvatarUrl.Substring(0, user.AvatarUrl.Length - 4);

						user.JoinDate = GetUnixTimestamp(player["timecreated"] != null ? (long)player["timecreated"] : 0);
						user.PrimaryGroupId = player["primaryclanid"] != null ? (string)player["primaryclanid"] : "";
						user.RealName = player["realname"] != null ? (string)player["realname"] : "";
						user.LocationCountryCode = player["loccountrycode"] != null ? (string)player["loccountrycode"] : "";
						user.LocationStateCode = player["locstatecode"] != null ? (string)player["locstatecode"] : "";
						user.LocationCityId = player["loccityid"] != null ? (int)player["loccityid"] : -1;

						users.Add(user);
					}

					// Requests are limited to 100 steamids, so issue multiple requests
					if (steamIds.Count > 100)
						users.AddRange(GetUserInfo(info, steamIds.GetRange(100, Math.Min(steamIds.Count - 100, 100))));

					return users;
				}
			}
			
			return null;
		}

		public List<User> GetUserInfo(AuthInfo info, List<Friend> friends)
		{
			List<string> steamIds = new List<string>();
			foreach (Friend f in friends) steamIds.Add(f.SteamId);
			return GetUserInfo(info, steamIds);
		}

		public User GetUserInfo(AuthInfo info, string steamId)
		{
			if (steamId == null) throw new ArgumentNullException("steamId")
			return GetUserInfo(info, new List<string>(new string[] { steamId }))[0];
		}

		/// <summary>
		/// Retrieve the avatar of the specified user in the specified format.
		/// </summary>
		/// <param name="user">User</param>
		/// <param name="size">Requested avatar size</param>
		/// <returns>The avatar as bitmap on success or null on failure.</returns>
		public Bitmap GetUserAvatar(User user, AvatarSize size = AvatarSize.Small)
		{
			if (user.AvatarUrl.Length == 0) return null;

			try
			{
				WebClient client = new WebClient();

				Stream stream;
				if (size == AvatarSize.Small)
					stream = client.OpenRead(user.AvatarUrl + ".jpg");
				else if (size == AvatarSize.Medium)
					stream = client.OpenRead(user.AvatarUrl + "_medium.jpg");
				else
					stream = client.OpenRead(user.AvatarUrl + "_full.jpg");

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

		/// <summary>
		/// Retrieve the avatar of the specified group in the specified format.
		/// </summary>
		/// <param name="group">Group</param>
		/// <param name="size">Requested avatar size</param>
		/// <returns>The avatar as bitmap on success or null on failure.</returns>
		public Bitmap GetGroupAvatar(GroupInfo group, AvatarSize size = AvatarSize.Small)
		{
			User user = new User();
			user.AvatarUrl = group.AvatarUrl;
			return GetUserAvatar(user, size);
		}

		/// <summary>
		/// Fetch all groups of a given user.
		/// </summary>
		/// <param name="steamid">SteamID</param>
		/// <returns>List of groups.</returns>
		//public List<Group> GetGroups(string steamid = null)
		//{
		//	if (umqid == null) return null;
		//	if (steamid == null) steamid = this.steamid;

		//	string response = steamRequest("ISteamUserOAuth/GetGroupList/v0001?access_token=" + accessToken + "&steamid=" + steamid);

		//	if (response != null)
		//	{
		//		JObject data = JObject.Parse(response);

		//		if (data["groups"] != null)
		//		{
		//			List<Group> groups = new List<Group>();

		//			foreach (JObject info in data["groups"])
		//			{
		//				Group group = new Group();

		//				group.steamid = (string)info["steamid"];
		//				group.inviteonly = ((string)info["permission"]).Equals("2");

		//				if (((string)info["relationship"]).Equals("Member"))
		//					groups.Add(group);
		//			}

		//			return groups;
		//		}
		//		else
		//		{
		//			return null;
		//		}
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

		/// <summary>
		/// Retrieve information about the specified groups.
		/// </summary>
		/// <param name="steamids">64-bit SteamIDs of groups</param>
		/// <returns>Information about the specified groups</returns>
		//public List<GroupInfo> GetGroupInfo(List<string> steamids)
		//{
		//	if (umqid == null) return null;

		//	string response = steamRequest("ISteamUserOAuth/GetGroupSummaries/v0001?access_token=" + accessToken + "&steamids=" + string.Join(",", steamids.GetRange(0, Math.Min(steamids.Count, 100)).ToArray()));

		//	if (response != null)
		//	{
		//		JObject data = JObject.Parse(response);

		//		if (data["groups"] != null)
		//		{
		//			List<GroupInfo> groups = new List<GroupInfo>();

		//			foreach (JObject info in data["groups"])
		//			{
		//				GroupInfo group = new GroupInfo();

		//				group.steamid = (string)info["steamid"];
		//				group.creationDate = unixTimestamp((long)info["timecreated"]);
		//				group.name = (string)info["name"];
		//				group.profileUrl = "http://steamcommunity.com/groups/" + (string)info["profileurl"];
		//				group.usersOnline = (int)info["usersonline"];
		//				group.usersInChat = (int)info["usersinclanchat"];
		//				group.usersInGame = (int)info["usersingame"];
		//				group.owner = (string)info["ownerid"];
		//				group.members = (int)info["users"];

		//				group.avatarUrl = (string)info["avatar"];
		//				if (group.avatarUrl != null) group.avatarUrl = group.avatarUrl.Substring(0, group.avatarUrl.Length - 4);

		//				group.headline = info["headline"] != null ? (string)info["headline"] : "";
		//				group.summary = info["summary"] != null ? (string)info["summary"] : "";
		//				group.abbreviation = info["abbreviation"] != null ? (string)info["abbreviation"] : "";
		//				group.locationCountryCode = info["loccountrycode"] != null ? (string)info["loccountrycode"] : "";
		//				group.locationStateCode = info["locstatecode"] != null ? (string)info["locstatecode"] : "";
		//				group.locationCityId = info["loccityid"] != null ? (int)info["loccityid"] : -1;
		//				group.favoriteAppId = info["favoriteappid"] != null ? (int)info["favoriteappid"] : -1;

		//				groups.Add(group);
		//			}

		//			// Requests are limited to 100 steamids, so issue multiple requests
		//			if (steamids.Count > 100)
		//				groups.AddRange(GetGroupInfo(steamids.GetRange(100, Math.Min(steamids.Count - 100, 100))));

		//			return groups;
		//		}
		//		else
		//		{
		//			return null;
		//		}
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

		//public List<GroupInfo> GetGroupInfo(List<Group> groups)
		//{
		//	List<string> steamids = new List<string>(groups.Count);
		//	foreach (Group g in groups) steamids.Add(g.SteamId);
		//	return GetGroupInfo(steamids);
		//}

		//public GroupInfo GetGroupInfo(string steamid)
		//{
		//	return GetGroupInfo(new List<string>(new string[] { steamid }))[0];
		//}

		/// <summary>
		/// Let a user know you're typing a message. Should be called periodically.
		/// </summary>
		/// <param name="steamid">Recipient of notification</param>
		/// <returns>Returns a boolean indicating success of the request.</returns>
		//public bool SendTypingNotification(User user)
		//{
		//	if (umqid == null) return false;

		//	string response = steamRequest("ISteamWebUserPresenceOAuth/Message/v0001", "?access_token=" + accessToken + "&umqid=" + umqid + "&type=typing&steamid_dst=" + user.steamid);

		//	if (response != null)
		//	{
		//		JObject data = JObject.Parse(response);

		//		return data["error"] != null && ((string)data["error"]).Equals("OK");
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}

		/// <summary>
		/// Send a text message to the specified user.
		/// </summary>
		/// <param name="steamid">Recipient of message</param>
		/// <param name="message">Message contents</param>
		/// <returns>Returns a boolean indicating success of the request.</returns>
		//public bool SendMessage(User user, string message)
		//{
		//	if (umqid == null) return false;

		//	string response = steamRequest("ISteamWebUserPresenceOAuth/Message/v0001", "?access_token=" + accessToken + "&umqid=" + umqid + "&type=saytext&text=" + Uri.EscapeDatastring(message) + "&steamid_dst=" + user.steamid);

		//	if (response != null)
		//	{
		//		JObject data = JObject.Parse(response);

		//		return data["error"] != null && ((string)data["error"]).Equals("OK");
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}

		//public bool SendMessage(string steamid, string message)
		//{
		//	User user = new User();
		//	user.steamid = steamid;
		//	return SendMessage(user, message);
		//}

		/// <summary>
		/// Check for updates and new messages.
		/// </summary>
		/// <returns>A list of updates.</returns>
		//public List<Update> Poll()
		//{
		//	if (umqid == null) return null;

		//	string response = steamRequest("ISteamWebUserPresenceOAuth/Poll/v0001", "?access_token=" + accessToken + "&umqid=" + umqid + "&message=" + message);

		//	if (response != null)
		//	{
		//		JObject data = JObject.Parse(response);

		//		if (((string)data["error"]).Equals("OK"))
		//		{
		//			message = (int)data["messagelast"];

		//			List<Update> updates = new List<Update>();

		//			foreach (JObject info in data["messages"])
		//			{
		//				Update update = new Update();

		//				update.timestamp = unixTimestamp((long)info["timestamp"]);
		//				update.origin = (string)info["steamid_from"];

		//				string type = (string)info["type"];
		//				if (type.Equals("saytext") || type.Equals("my_saytext") || type.Equals("emote"))
		//				{
		//					update.type = type.Equals("emote") ? UpdateType.Emote : UpdateType.Message;
		//					update.message = (string)info["text"];
		//					update.localMessage = type.Equals("my_saytext");
		//				}
		//				else if (type.Equals("typing"))
		//				{
		//					update.type = UpdateType.TypingNotification;
		//					update.message = (string)info["text"]; // Not sure if this is useful
		//				}
		//				else if (type.Equals("personastate"))
		//				{
		//					update.type = UpdateType.UserUpdate;
		//					update.status = (UserStatus)(int)info["persona_state"];
		//					update.nick = (string)info["persona_name"];
		//				}
		//				else
		//				{
		//					continue;
		//				}

		//				updates.Add(update);
		//			}

		//			return updates;
		//		}
		//		else
		//		{
		//			return null;
		//		}
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

		/// <summary>
		/// Retrieves information about the server.
		/// </summary>
		/// <returns>Returns a structure with the information.</returns>
		public ServerInfo GetServerInfo()
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

		private static WebRequest GetDefaultRequest(string path)
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
		private static string GetApiResponse(string path)
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

		private static string PostApiResponse(string path, string postData)
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

				magicalCounterOfUnknownPurpose++;

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

		private static DateTime GetUnixTimestamp(long timestamp)
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
