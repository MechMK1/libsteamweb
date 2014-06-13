using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechMK1.SteamWeb
{
	[Serializable]
	public class LoginException : Exception
	{
		public LoginException() { }
		public LoginException(string message) : base(message) { }
		public LoginException(string message, Exception inner) : base(message, inner) { }
		protected LoginException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
