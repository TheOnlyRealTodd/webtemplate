using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.Utils;
using Clifton.WebInterfaces;

using WebsiteTemplate.Models;

namespace WebsiteTemplate
{
	static partial class Program
	{
		static void Main(string[] args)
		{
			Bootstrap();

			try
			{
				InitializeDatabaseContext();
				InitializeRoutes();
				StartWebServer();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				Exception ex2 = ex;

				while (ex2.InnerException != null)
				{
					ex2 = ex2.InnerException;
					Console.WriteLine("---------- INNER EXCEPTION -----------");
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
			}

			Console.ReadLine();
		}

		private static void InitializeRoutes()
		{
			IAuthenticatingRouterService authRouter = serviceManager.Get<IAuthenticatingRouterService>();
			authRouter.RegisterSemanticRoute<HtmlPageRoute>("GET:", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<HtmlPageRoute>("GET:account/login", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<Login>("POST:account/login", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<Register>("POST:account/register", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<ForgotPassword>("POST:account/forgotPassword", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<Logout>("GET:account/logout", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<RegistrationToken>("GET:account/finishRegistration", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<ForgotPasswordToken>("GET:account/recoverPassword", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<NewPassword>("POST:account/recoverPassword", RouteType.PublicRoute);
			authRouter.RegisterSemanticRoute<ViewAccountInfo>("GET:account/updateAccountInfo", RouteType.AuthenticatedRoute);
			authRouter.RegisterSemanticRoute<UpdateAccountInfo>("POST:account/updateAccountInfo", RouteType.AuthenticatedRoute);
			authRouter.RegisterSemanticRoute<HtmlPageRoute>("GET:account/updateAccountInfo", RouteType.RoleRoute, Role.SuperAdmin);

			authRouter.RegisterSemanticRoute<HtmlPageRoute>("GET:authtest");
		}

		private static void StartWebServer()
		{
			ISemanticProcessor semProc = serviceManager.Get<ISemanticProcessor>();
			semProc.Register<WebServerMembrane, AccountRouteReceptor>();

			IWebServerService server = serviceManager.Get<IWebServerService>();
			IAppConfigService configService = serviceManager.Get<IAppConfigService>();
			string ip = configService.GetValue("ip");
			string ports = configService.GetValue("ports");
			int[] portVals = ports.Split(',').Select(p => p.Trim().to_i()).ToArray();
			server.Start(ip, portVals);
		}

		private static void InitializeDatabaseContext()
		{
			SqlConnection connection = new SqlConnection(serviceManager.Get<IAppConfigService>().GetConnectionString("db"));
			SiteDataContext context = new SiteDataContext(connection);
			IDbContextService db = serviceManager.Get<IDbContextService>();
			db.InitializeContext(context);
			db.CreateDatabaseAndTablesIfNotExists();
		}
	}
}
