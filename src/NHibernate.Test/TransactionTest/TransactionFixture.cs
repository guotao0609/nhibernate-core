using System;
using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;

namespace NHibernate.Test.TransactionTest
{
	[TestFixture]
	public class TransactionFixture : TransactionFixtureBase
	{
		[Test]
		public void SecondTransactionShouldntBeCommitted()
		{
			using (ISession session = OpenSession())
			{
				using (ITransaction t1 = session.BeginTransaction())
				{
					t1.Commit();
				}

				using (ITransaction t2 = session.BeginTransaction())
				{
					Assert.IsFalse(t2.WasCommitted);
					Assert.IsFalse(t2.WasRolledBack);
				}
			}
		}

		[Test]
		public void CommitAfterDisposeThrowsException()
		{
			using (ISession s = OpenSession())
			{
				ITransaction t = s.BeginTransaction();
				t.Dispose();
				Assert.Throws<ObjectDisposedException>(() => t.Commit());
			}
		}

		[Test]
		public void RollbackAfterDisposeThrowsException()
		{
			using (ISession s = OpenSession())
			{
				ITransaction t = s.BeginTransaction();
				t.Dispose();
				Assert.Throws<ObjectDisposedException>(() => t.Rollback());
			}
		}

		[Test]
		public void EnlistAfterDisposeDoesNotThrowException()
		{
			using (ISession s = OpenSession())
			{
				ITransaction t = s.BeginTransaction();

				using (var cmd = s.Connection.CreateCommand())
				{
					t.Dispose();
					t.Enlist(cmd);
				}
			}
		}

		[Test]
		public void CommandAfterTransactionShouldWork()
		{
			using (ISession s = OpenSession())
			{
				using (s.BeginTransaction())
				{
				}

				s.CreateQuery("from Person").List();

				using (ITransaction t = s.BeginTransaction())
				{
					t.Commit();
				}

				s.CreateQuery("from Person").List();

				using (ITransaction t = s.BeginTransaction())
				{
					t.Rollback();
				}

				s.CreateQuery("from Person").List();
			}
		}

		[Test]
		public void WasCommittedOrRolledBack()
		{
			using (ISession s = OpenSession())
			{
				using (ITransaction t = s.BeginTransaction())
				{
					Assert.AreSame(t, s.Transaction);
					Assert.IsFalse(s.Transaction.WasCommitted);
					Assert.IsFalse(s.Transaction.WasRolledBack);
					t.Commit();

					// ISession.Transaction returns a new transaction
					// if the previous one completed!
					Assert.IsNotNull(s.Transaction);
					Assert.IsFalse(t == s.Transaction);

					Assert.IsTrue(t.WasCommitted);
					Assert.IsFalse(t.WasRolledBack);
					Assert.IsFalse(s.Transaction.WasCommitted);
					Assert.IsFalse(s.Transaction.WasRolledBack);
					Assert.IsFalse(t.IsActive);
					Assert.IsFalse(s.Transaction.IsActive);
				}

				using (ITransaction t = s.BeginTransaction())
				{
					t.Rollback();

					// ISession.Transaction returns a new transaction
					// if the previous one completed!
					Assert.IsNotNull(s.Transaction);
					Assert.IsFalse(t == s.Transaction);

					Assert.IsTrue(t.WasRolledBack);
					Assert.IsFalse(t.WasCommitted);

					Assert.IsFalse(s.Transaction.WasCommitted);
					Assert.IsFalse(s.Transaction.WasRolledBack);

					Assert.IsFalse(t.IsActive);
					Assert.IsFalse(s.Transaction.IsActive);
				}
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
				using (var t = s.BeginTransaction())
				{
					var p1 = new Person();
					// The relationship is there for failing in case the flush ordering is not the expected one.
					var p2 = new Person { Related = p1 };
					var p3 = new Person { Related = p2 };
					s1.Save(p1);
					s2.Save(p2);
					s.Save(p3);
					t.Commit();
				}
			}

			using (var s = OpenSession())
			using (var t = s.BeginTransaction())
			{
				Assert.That(s.Query<Person>().Count(), Is.EqualTo(3));
				Assert.That(s.Query<Person>().Count(p => p.Related != null), Is.EqualTo(2));
				t.Commit();
			}
		}
	}
}