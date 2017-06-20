using System.Collections;
using System.Linq;
using NHibernate.Linq;

namespace NHibernate.Test.TransactionTest
{
	public abstract class TransactionFixtureBase : TestCase
	{
		protected override IList Mappings => new[] { "TransactionTest.Person.hbm.xml" };

		protected override string MappingsAssembly => "NHibernate.Test";

		protected override void OnTearDown()
		{
			using (var s = OpenSession())
			using (var t = s.BeginTransaction())
			{
				// MySql and maybe some other db fails deleting the whole table in "right order"
				foreach (var p in s.Query<Person>().Where(p => p.Related.Related != null))
				{
					s.Delete(p);
				}
				s.Flush();
				s.CreateQuery("delete from Person p where p.Related != null").ExecuteUpdate();
				s.CreateQuery("delete from System.Object").ExecuteUpdate();
				t.Commit();
			}
		}
	}
}