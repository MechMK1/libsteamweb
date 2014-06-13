using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MechMK1.SteamWeb
{
	[Serializable]
	public class LoginSteamGuardException : LoginException
	{
		public LoginSteamGuardException() { }
		public LoginSteamGuardException(string message) : base(message) { }
		public LoginSteamGuardException(string message, Exception inner) : base(message, inner) { }
		protected LoginSteamGuardException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
