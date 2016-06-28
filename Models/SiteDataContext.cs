using System;
using System.Data.Common;
using System.Linq;
using System.Data.Linq;

namespace WebsiteTemplate.Models
{
	public class SiteDataContext : DataContext
	{
		public SiteDataContext(DbConnection conn)
			: base(conn)
		{
		}

		public Table<UserAccount> UserAccount { get { return GetTable<UserAccount>(); } }
	}
}
