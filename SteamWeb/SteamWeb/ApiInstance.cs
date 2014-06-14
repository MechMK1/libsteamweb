using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	/// <summary>
	/// 
	/// </summary>
	public class ApiInstance
	{
		private int lastKnownMessageCounter = 0;
		private AuthInfo authInfo;

		#region Constructor and Creator
		private ApiInstance(AuthInfo info)
		{
			this.authInfo = info;
		}
		public ApiInstance CreateApiInstance(AuthInfo info)
		{
			if (info.UmqId != null && info.SteamId != null)
			{
				return new ApiInstance(info);
			}
			else throw new ArgumentException("Invalid authentification info", "info");
		}
		#endregion

		#region API Methods
		/// <summary>
		/// Fetch all friends of a given user.
		/// </summary>
		/// <remarks>This function does not provide detailed information.</remarks>
		/// <param name="steamId">steamid of target user or self</param>
		/// <returns>List of friends or null on failure.</returns>
		public List<Friend> GetFriends(string steamId)
		{
			if (this.authInfo.UmqId == null) return null;
			if (steamId == null) return null;

			string response = API.GetApiResponse("ISteamUserOAuth/GetFriendList/v0001?access_token=" + this.authInfo.AccessToken + "&steamid=" + steamId);

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
						f.FriendSince = API.GetUnixTimestamp((long)friend["friend_since"]);
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
		public List<User> GetUserInfo(List<string> steamIds)
		{
			if (this.authInfo.UmqId == null) return null;

			string response = API.GetApiResponse(
				"ISteamUserOAuth/GetUserSummaries/v0001?access_token=" + this.authInfo.AccessToken +
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
						user.LastLogoff = API.GetUnixTimestamp((long)player["lastlogoff"]);
						user.ProfileUrl = (string)player["profileurl"];
						user.Status = (UserStatus)(int)player["personastate"];

						user.AvatarUrl = player["avatar"] != null ? (string)player["avatar"] : "";
						if (user.AvatarUrl != null) user.AvatarUrl = user.AvatarUrl.Substring(0, user.AvatarUrl.Length - 4);

						user.JoinDate = API.GetUnixTimestamp(player["timecreated"] != null ? (long)player["timecreated"] : 0);
						user.PrimaryGroupId = player["primaryclanid"] != null ? (string)player["primaryclanid"] : "";
						user.RealName = player["realname"] != null ? (string)player["realname"] : "";
						user.LocationCountryCode = player["loccountrycode"] != null ? (string)player["loccountrycode"] : "";
						user.LocationStateCode = player["locstatecode"] != null ? (string)player["locstatecode"] : "";
						user.LocationCityId = player["loccityid"] != null ? (int)player["loccityid"] : -1;

						users.Add(user);
					}

					// Requests are limited to 100 steamids, so issue multiple requests
					if (steamIds.Count > 100)
						users.AddRange(GetUserInfo(steamIds.GetRange(100, Math.Min(steamIds.Count - 100, 100))));

					return users;
				}
			}

			return null;
		}

		public List<User> GetUserInfo(AuthInfo info, List<Friend> friends)
		{
			List<string> steamIds = new List<string>();
			foreach (Friend f in friends) steamIds.Add(f.SteamId);
			return GetUserInfo(steamIds);
		}

		public User GetUserInfo(AuthInfo info, string steamId)
		{
			if (steamId == null) throw new ArgumentNullException("steamId");
			return GetUserInfo(new List<string>(new string[] { steamId }))[0];
		}

		/// <summary>
		/// Fetch all groups of a given user.
		/// </summary>
		/// <param name="steamid">SteamID</param>
		/// <returns>List of groups.</returns>
		public static List<Group> GetGroups(AuthInfo info, string steamId)
		{
			if (info.UmqId == null) return null;
			if (steamId == null) return null;

			string response = API.GetApiResponse("ISteamUserOAuth/GetGroupList/v0001?access_token=" + info.AccessToken + "&steamid=" + steamId);

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["groups"] != null)
				{
					List<Group> groups = new List<Group>();

					foreach (JObject g in data["groups"])
					{
						Group group = new Group();

						group.SteamId = (string)g["steamid"];
						group.InviteOnly = ((string)g["permission"]).Equals("2");

						if (((string)g["relationship"]).Equals("Member"))
							groups.Add(group);
					}

					return groups;
				}
			}

			return null;
		}

		/// <summary>
		/// Retrieve information about the specified groups.
		/// </summary>
		/// <param name="steamids">64-bit SteamIDs of groups</param>
		/// <returns>Information about the specified groups</returns>
		public static List<GroupInfo> GetGroupInfo(AuthInfo info, List<string> steamIds)
		{
			if (info.UmqId == null) return null;

			string response = API.GetApiResponse("ISteamUserOAuth/GetGroupSummaries/v0001?access_token=" + info.AccessToken + "&steamids=" + string.Join(",", steamIds.GetRange(0, Math.Min(steamIds.Count, 100)).ToArray()));

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (data["groups"] != null)
				{
					List<GroupInfo> groups = new List<GroupInfo>();

					foreach (JObject g in data["groups"])
					{
						GroupInfo group = new GroupInfo();

						group.SteamId = (string)g["steamid"];
						group.CreationDate = API.GetUnixTimestamp((long)g["timecreated"]);
						group.Name = (string)g["name"];
						group.ProfileUrl = "http://steamcommunity.com/groups/" + (string)g["profileurl"];
						group.UsersOnline = (int)g["usersonline"];
						group.UsersInChat = (int)g["usersinclanchat"];
						group.UsersInGame = (int)g["usersingame"];
						group.Owner = (string)g["ownerid"];
						group.Members = (int)g["users"];

						group.AvatarUrl = (string)g["avatar"];
						if (group.AvatarUrl != null) group.AvatarUrl = group.AvatarUrl.Substring(0, group.AvatarUrl.Length - 4);

						group.Headline = g["headline"] != null ? (string)g["headline"] : "";
						group.Summary = g["summary"] != null ? (string)g["summary"] : "";
						group.Abbreviation = g["abbreviation"] != null ? (string)g["abbreviation"] : "";
						group.LocationCountryCode = g["loccountrycode"] != null ? (string)g["loccountrycode"] : "";
						group.LocationStateCode = g["locstatecode"] != null ? (string)g["locstatecode"] : "";
						group.LocationCityId = g["loccityid"] != null ? (int)g["loccityid"] : -1;
						group.FavoriteAppId = g["favoriteappid"] != null ? (int)g["favoriteappid"] : -1;

						groups.Add(group);
					}

					// Requests are limited to 100 steamids, so issue multiple requests
					if (steamIds.Count > 100)
						groups.AddRange(GetGroupInfo(info, steamIds.GetRange(100, Math.Min(steamIds.Count - 100, 100))));

					return groups;
				}
			}

			return null;
		}

		public static List<GroupInfo> GetGroupInfo(AuthInfo info, List<Group> groups)
		{
			List<string> steamIds = new List<string>();
			foreach (Group g in groups) steamIds.Add(g.SteamId);
			return GetGroupInfo(info, steamIds);
		}

		public static GroupInfo GetGroupInfo(AuthInfo info, string steamId)
		{
			return GetGroupInfo(info, new List<string>(new string[] { steamId }))[0];
		}

		/// <summary>
		/// Let a user know you're typing a message. Should be called periodically.
		/// </summary>
		/// <param name="steamid">Recipient of notification</param>
		/// <returns>Returns a boolean indicating success of the request.</returns>
		public bool SendTypingNotification(AuthInfo info, User user)
		{
			if (info.UmqId == null) return false;

			string response = PostToApi("ISteamWebUserPresenceOAuth/Message/v0001", "?access_token=" + info.AccessToken + "&umqid=" + info.UmqId + "&type=typing&steamid_dst=" + user.SteamId);

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				return data["error"] != null && ((string)data["error"]).Equals("OK");
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Send a text message to the specified user.
		/// </summary>
		/// <param name="steamid">Recipient of message</param>
		/// <param name="message">Message contents</param>
		/// <returns>Returns a boolean indicating success of the request.</returns>
		public bool SendMessage(AuthInfo info, User user, string message)
		{
			if (info.UmqId == null) return false;

			string response = PostToApi("ISteamWebUserPresenceOAuth/Message/v0001", "?access_token=" + info.AccessToken + "&umqid=" + info.UmqId + "&type=saytext&text=" + Uri.EscapeDataString(message) + "&steamid_dst=" + user.SteamId);

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				return data["error"] != null && ((string)data["error"]).Equals("OK");
			}
			else
			{
				return false;
			}
		}

		public bool SendMessage(AuthInfo info, string steamId, string message)
		{
			User user = new User();
			user.SteamId = steamId;
			return SendMessage(info, user, message);
		}

		/// <summary>
		/// Check for updates and new messages.
		/// </summary>
		/// <returns>A list of updates.</returns>
		public List<Update> Poll(AuthInfo info)
		{
			if (info.UmqId == null) return null;

			string response = PostToApi("ISteamWebUserPresenceOAuth/Poll/v0001", "?access_token=" + info.AccessToken + "&umqid=" + info.UmqId + "&message=" + this.lastKnownMessageCounter);

			if (response != null)
			{
				JObject data = JObject.Parse(response);

				if (((string)data["error"]).Equals("OK"))
				{
					this.lastKnownMessageCounter = (int)data["messagelast"];

					List<Update> updates = new List<Update>();

					foreach (JObject g in data["messages"])
					{
						Update update = new Update();

						update.Timestamp = API.GetUnixTimestamp((long)g["timestamp"]);
						update.Origin = (string)g["steamid_from"];

						string type = (string)g["type"];
						if (type.Equals("saytext") || type.Equals("my_saytext") || type.Equals("emote"))
						{
							update.Type = type.Equals("emote") ? UpdateType.Emote : UpdateType.Message;
							update.Message = (string)g["text"];
							update.LocalMessage = type.Equals("my_saytext");
						}
						else if (type.Equals("typing"))
						{
							update.Type = UpdateType.TypingNotification;
							update.Message = (string)g["text"]; // Not sure if this is useful
						}
						else if (type.Equals("personastate"))
						{
							update.Type = UpdateType.UserUpdate;
							update.Status = (UserStatus)(int)g["persona_state"];
							update.Nick = (string)g["persona_name"];
						}
						else
						{
							continue;
						}

						updates.Add(update);
					}

					return updates;
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

		#endregion

		private string PostToApi(string path, string postData)
		{
			WebRequest request = API.GetDefaultRequest(path);
			try
			{
				request.Method = "POST";
				byte[] postBytes = Encoding.ASCII.GetBytes(postData);
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = postBytes.Length;

				Stream requestStream = request.GetRequestStream();
				requestStream.Write(postBytes, 0, postBytes.Length);
				requestStream.Close();

				this.lastKnownMessageCounter++;

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
	}
}
