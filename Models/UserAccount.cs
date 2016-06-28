using System;
using System.Data.Linq.Mapping;
using System.Linq;

using Clifton.Core.ModelTableManagement;
using Clifton.Core.ServiceInterfaces;

namespace WebsiteTemplate.Models
{
	[Table]
	public class UserAccount : IEntity
	{
		// Stupid Linq2Sql can't have common propeties in a base class!

		// SQLite support.
		// Can't use IsDbGenerated attribute.
		[Column(IsPrimaryKey = true)]
		public int? Id { get; set; }            // must be nullable for auto-increment to work.

		[Column]
		public string FirstName { get; set; }

		[Column]
		public string LastName { get; set; }

		[Column]
		public string UserName { get; set; }

		[Column]
		public string Email { get; set; }

		[Column]
		public string PasswordHash { get; set; }

		[Column]
		public string RegistrationToken { get; set; }

		[Column]
		public string PasswordRecoveryToken { get; set; }

		[Column]
		public bool Registered { get; set; }

		[Column]
		public long UtcLastLogin { get; set; }

		[Column]
		public int RoleMask { get; set; }

		public string FullName
		{
			get { return (FirstName ?? "") + " " + (LastName ?? ""); }
		}
	}
}
