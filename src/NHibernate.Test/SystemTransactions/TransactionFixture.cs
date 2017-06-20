using System.Linq;
using System.Transactions;
using NHibernate.Linq;
using NHibernate.Test.TransactionTest;
using NUnit.Framework;

namespace NHibernate.Test.SystemTransactions
{
	[TestFixture]
	public class TransactionFixture : TransactionFixtureBase
	{
		[Test]
		public void CanUseSystemTransactionsToCommit()
		{
			int identifier;
			using(ISession session = Sfi.OpenSession())
			using(TransactionScope tx = new TransactionScope())
			{
				var s = new Person();
				session.Save(s);
				identifier = s.Id;
				tx.Complete();
			}

			using (ISession session = Sfi.OpenSession())
			using (TransactionScope tx = new TransactionScope())
			{
				var w = session.Get<Person>(identifier);
				Assert.IsNotNull(w);
				session.Delete(w);
				tx.Complete();
			}
		}

		[Test]
		public void FlushFromTransactionAppliesToDisposedSharingSession()
		{
			using (var s = OpenSession())
			{
				var builder = s.SessionWithOptions().Connection();

				using (var t = new TransactionScope())
				{
					var p1 = new Person();
					// The relationship is there for failing in case the flush ordering is not the expected one.
					var p2 = new Person { Related = p1 };
					var p3 = new Person { Related = p2 };
					// If putting p3 here, adjust base tear-down.
					var p4 = new Person { Related = p2 };

					using (var s1 = builder.OpenSession())
						s1.Save(p1);
					using (var s2 = builder.OpenSession())
					{
						s2.Save(p2);
						using (var s3 = s2.SessionWithOptions().Connection().OpenSession())
							s3.Save(p3);
					}
					s.Save(p4);
					t.Complete();
				}
			}

			using (var s = OpenSession())
			using (var t = s.BeginTransaction())
			{
				Assert.That(s.Query<Person>().Count(), Is.EqualTo(4));
				Assert.That(s.Query<Person>().Count(p => p.Related != null), Is.EqualTo(3));
				t.Commit();
			}
		}

		[Test]
		public void FlushFromTransactionAppliesToSharingSession()
		{
			using (var s = OpenSession())
			{
				var builder = s.SessionWithOptions().Connection();

				using (var s1 = builder.OpenSession())
				using (var s2 = builder.OpenSession())
				using (var s3 = s2.SessionWithOptions().Connection().OpenSession())
				using (var t = new TransactionScope())
				{
					var p1 = new Person();
					// The relationship is there for failing in case the flush ordering is not the expected one.
					var p2 = new Person { Related = p1 };
					var p3 = new Person { Related = p2 };
					// If putting p3 here, adjust base tear-down.
					var p4 = new Person { Related = p2 };
					s1.Save(p1);
					s2.Save(p2);
					s3.Save(p3);
					s.Save(p4);
					t.Complete();
				}
			}

			using (var s = OpenSession())
			using (var t = s.BeginTransaction())
			{
				Assert.That(s.Query<Person>().Count(), Is.EqualTo(4));
				Assert.That(s.Query<Person>().Count(p => p.Related != null), Is.EqualTo(3));
				t.Commit();
			}
		}
	}
}