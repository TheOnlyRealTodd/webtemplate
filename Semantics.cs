using Clifton.Core.Semantics;
using Clifton.WebInterfaces;

namespace WebsiteTemplate
{
	public class Login : SemanticRoute
	{
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class Register : SemanticRoute
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class UpdateAccountInfo : SemanticRoute
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class RegistrationToken : SemanticRoute
	{
		public string Token { get; set; }
	}

	public class ForgotPasswordToken : SemanticRoute
	{
		public string Token { get; set; }
	}

	public class NewPassword : SemanticRoute
	{
		public string Password { get; set; }
	}

	public class Logout : SemanticRoute { }

	public class ForgotPassword : SemanticRoute
	{
		public string Email { get; set; }
	}

	public class ViewAccountInfo : SemanticRoute { }
}
