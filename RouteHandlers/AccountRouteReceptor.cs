using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.Utils;
using Clifton.WebInterfaces;

using WebsiteTemplate.Models;

namespace WebsiteTemplate
{
	public class AccountRouteReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, Login login)
		{
			IDbContextService db = proc.ServiceManager.Get<IDbContextService>();
			List<UserAccount> uas = db.Context.Query<UserAccount>(r => r.Email == login.Email);

			if (uas.Count == 1)
			{
				if (PasswordHash.ValidatePassword(login.Password, uas[0].PasswordHash))
				{
					if (uas[0].Registered)
					{
						proc.ServiceManager.Get<IWebSessionService>().Authenticate(login.Context);
						JsonResponse(proc, login, "{'state': 'OK'}");
						IWebSessionService session = proc.ServiceManager.Get<IWebSessionService>();
						session.SetSessionObject(login.Context, "OneTimeAlert", "Welcome Back " + uas[0].FirstName + "!");
						session.SetSessionObject(login.Context, "UserName", uas[0].FullName);
						session.SetSessionObject(login.Context, "UserAccount", uas[0]);
						session.SetSessionObject(login.Context, "RoleMask", uas[0].RoleMask);
					}
					else
					{
						JsonResponse(proc, login, "{'state': 'RegisterFirst'}");
					}
				}
				else
				{
					JsonResponse(proc, login, "{'state': 'BadAccount'}");
				}
			}
			else
			{
				JsonResponse(proc, login, "{'state': 'NotFound'}");
			}
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, Logout logout)
		{
			proc.ServiceManager.Get<IWebSessionService>().Logout(logout.Context);
			proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(logout.Context, "OneTimeAlert", "You are now logged out.");
			proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(logout.Context, "UserName", "");
			logout.Context.Redirect("/");
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, Register register)
		{
			IDbContextService db = proc.ServiceManager.Get<IDbContextService>();

			if (db.RecordExists<UserAccount>(r => r.Email == register.Email))
			{
				JsonResponse(proc, register, "{'state': 'UserExists'}");
			}
			else
			{
				UserAccount ua = new UserAccount()
				{
					FirstName = register.FirstName,
					LastName = register.LastName,
					Email = register.Email,
					PasswordHash = PasswordHash.CreateHash(register.Password),
					RegistrationToken = Guid.NewGuid().ToString(),
					RoleMask = 1,
				};

				string website = proc.ServiceManager.Get<IConfigService>().GetValue("website");

				// Email the registrant.
				proc.ServiceManager.Get<ISemanticProcessor>().ProcessInstance<EmailClientMembrane, Email>(email =>
				{
					email.AddTo(register.Email);
					email.Subject = "ByteStruck Registration";
					email.Body = File.ReadAllText("registrationEmail.html").Replace("{link}", website + "/account/finishRegistration?token=" + ua.RegistrationToken);
				});

				// Insert the record as unregistered, with the registration token.
				db.Context.Insert(ua);

				JsonResponse(proc, register, "{'state': 'OK'}");
			}
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, RegistrationToken token)
		{
			IDbContextService db = proc.ServiceManager.Get<IDbContextService>();
			List<UserAccount> uas = db.Context.Query<UserAccount>(r => r.RegistrationToken == token.Token);
			bool validated = uas.Count == 1;
			proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(token.Context, "TokenValidated", validated);

			if (validated)
			{
				uas[0].RegistrationToken = null;
				uas[0].Registered = true;
				db.Context.Update(uas[0]);
			}

			// Continue processing the get request to display the /account/finishRegistration page.
			proc.ProcessInstance<WebServerMembrane, UnhandledContext>(c => c.Context = token.Context);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ForgotPassword forgotPassword)
		{
			IDbContextService db = proc.ServiceManager.Get<IDbContextService>();
			List<UserAccount> uas = db.Context.Query<UserAccount>(r => r.Email == forgotPassword.Email);

			if (uas.Count == 1)
			{
				uas[0].PasswordRecoveryToken = Guid.NewGuid().ToString();
				db.Context.Update(uas[0]);

				string website = proc.ServiceManager.Get<IConfigService>().GetValue("website");

				// Email the registrant.
				proc.ServiceManager.Get<ISemanticProcessor>().ProcessInstance<EmailClientMembrane, Email>(email =>
				{
					email.AddTo(forgotPassword.Email);
					email.Subject = "ByteStruck Password Recovery";
					email.Body = File.ReadAllText("passwordRecoveryEmail.html").Replace("{link}", website + "/account/recoverPassword?token=" + uas[0].PasswordRecoveryToken);
				});

				JsonResponse(proc, forgotPassword, "{'state': 'OK'}");
			}
			else
			{
				JsonResponse(proc, forgotPassword, "{'state': 'NotFound'}");
			}
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ForgotPasswordToken token)
		{
			IDbContextService db = proc.ServiceManager.Get<IDbContextService>();
			List<UserAccount> uas = db.Context.Query<UserAccount>(r => r.PasswordRecoveryToken == token.Token);
			bool validated = uas.Count == 1;

			if (validated)
			{
				proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(token.Context, "UserPasswordRecoveryId", uas[0].Id);
				uas[0].PasswordRecoveryToken = null;
				db.Context.Update(uas[0]);
			}
			else
			{
				proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(token.Context, "UserPasswordRecoveryId", null);
			}

			// Continue processing the get request to display the /account/passwordRecovery page.
			proc.ProcessInstance<WebServerMembrane, UnhandledContext>(c => c.Context = token.Context);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, NewPassword token)
		{
			int? id = proc.ServiceManager.Get<IWebSessionService>().GetSessionObject<int?>(token.Context, "UserPasswordRecoveryId");

			if (id != null)
			{
				IDbContextService db = proc.ServiceManager.Get<IDbContextService>();
				List<UserAccount> uas = db.Context.Query<UserAccount>(r => r.Id == (int)id);

				if (uas.Count == 1)
				{
					uas[0].PasswordHash = PasswordHash.CreateHash(token.Password);
					uas[0].PasswordRecoveryToken = null;
					db.Context.Update(uas[0]);
					proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(token.Context, "OneTimeAlert", "Your password has been reset.  Please login.");
					proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(token.Context, "UserPasswordRecoveryId", null);
					JsonResponse(proc, token, "{'state': 'OK'}");
				}
			}
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ViewAccountInfo token)
		{
			// Thought we'd have to do something here to fill in the form, but nope.  The current UserAccount is set in the session store, so we don't have to do anything here.
			// Continue processing the get request to display the /account/passwordRecovery page.
			proc.ProcessInstance<WebServerMembrane, UnhandledContext>(c => c.Context = token.Context);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, UpdateAccountInfo acctInfo)
		{
			IWebSessionService session = proc.ServiceManager.Get<IWebSessionService>();
			UserAccount ua = session.GetSessionObject<UserAccount>(acctInfo.Context, "UserAccount");
			ua.FirstName = acctInfo.FirstName;
			ua.LastName = acctInfo.LastName;
			ua.Email = acctInfo.Email;
			session.SetSessionObject(acctInfo.Context, "UserName", ua.FullName);

			if (!String.IsNullOrEmpty(acctInfo.Password))
			{
				ua.PasswordHash = PasswordHash.CreateHash(acctInfo.Password);
			}

			IDbContextService db = proc.ServiceManager.Get<IDbContextService>();
			db.Context.Update(ua);
			proc.ServiceManager.Get<IWebSessionService>().SetSessionObject(acctInfo.Context, "OneTimeAlert", "Your account information has been updated.");
			JsonResponse(proc, acctInfo, "{'state': 'OK'}");
		}

		protected void JsonResponse(ISemanticProcessor proc, SemanticRoute packet, string jsonResp)
		{
			proc.ProcessInstance<WebServerMembrane, JsonResponse>(r =>
			{
				r.Context = packet.Context;
				// Here we return the key and value with single quotes.  If we use double quotes, jQuery post will fail!
				// But the resulting value cannot be parsed in Javascript because a JSON string must have double-quotes for the key and value.
				// WTF????
				r.Json = jsonResp.ExchangeQuoteSingleQuote();
				r.StatusCode = 200;
			});
		}
	}
}
