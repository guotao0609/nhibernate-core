﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NHibernate.Cfg;
using NHibernate.Impl;
using NHibernate.Util;
using NUnit.Framework;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Test.NHSpecificTest.GH1391
{
	using System.Linq;
	[TestFixture]
	public class FixtureAsync
	{
		private readonly Random _random = new Random();
		
		[Test]
		public Task ConcurrentAsync()
		{
			// Simulating two session factories, where one has tracking enabled and the other disabled
			return Task.WhenAll(Enumerable.Range(0, 100).Select(i =>
			{
				if (_random.Next(2) == 0)
				{
					return EnabledAsync();
				}
				else
				{
					return DisabledAsync();
				}
			}));
		}

		[Test]
		public void ConcurrentAsync()
		{
			async Task RunAsync(bool enabled)
			{
				for (var i = 0; i < 50; i++)
				{
					if (enabled)
					{
						await EnabledAsync().ConfigureAwait(false);
					}
					else
					{
						await DisabledAsync().ConfigureAwait(false);
					}
				}
			}
			// Simulating two session factories, where one has tracking enabled and the other disabled
			Task.WaitAll(RunAsync(true), RunAsync(false));
		}
		
		[Test]
		public void Enabled()
		{
			var guid = Guid.NewGuid();
			using (new SessionIdLoggingContext(guid))
			{
				Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid));
				var guid2 = Guid.NewGuid();
				using (new SessionIdLoggingContext(guid2))
				{
					Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid2));
				}
				Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid));
			}
			Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
		}
		
		[Test]
		public async Task EnabledAsync()
		{
			var guid = Guid.NewGuid();
			using (new SessionIdLoggingContext(guid))
			{
				Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid));
				await Task.Delay(1).ConfigureAwait(false);
				Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid));

				var guid2 = Guid.NewGuid();
				using (new SessionIdLoggingContext(guid2))
				{
					Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid2));
					await Task.Delay(1).ConfigureAwait(false);
					Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid2));
				}
				Assert.That(SessionIdLoggingContext.SessionId, Is.EqualTo(guid));
			}
			Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
		}

		[Test]
		public void Disabled()
		{
			var guid = Guid.Empty;
			using (new SessionIdLoggingContext(guid))
			{
				Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
				using (new SessionIdLoggingContext(guid))
				{
					Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
				}
				Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
			}
			Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
		}
		
		[Test]
		public async Task DisabledAsync()
		{
			var guid = Guid.Empty;
			using (new SessionIdLoggingContext(guid))
			{
				Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
				await Task.Delay(1).ConfigureAwait(false);
				Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
				
				using (new SessionIdLoggingContext(guid))
				{
					Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
					await Task.Delay(1).ConfigureAwait(false);
					Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
				}
				Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
			}
			Assert.That(SessionIdLoggingContext.SessionId, Is.Null);
		}
	}
}
