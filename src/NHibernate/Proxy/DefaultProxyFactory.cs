using System;
using System.Collections.Concurrent;
using System.Reflection;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Proxy
{
	public class DefaultProxyFactory : AbstractProxyFactory
	{
		static readonly ConcurrentDictionary<ProxyCacheEntry, TypeInfo> Cache = new ConcurrentDictionary<ProxyCacheEntry, TypeInfo>();

		protected static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof (DefaultProxyFactory));

		public override INHibernateProxy GetProxy(object id, ISessionImplementor session)
		{
			try
			{
				var proxyBuilder = new NHibernateProxyBuilder(GetIdentifierMethod, SetIdentifierMethod, ComponentIdType, OverridesEquals);
				var cacheEntry = new ProxyCacheEntry(IsClassProxy ? PersistentClass : typeof(object), Interfaces);
				var proxyType = Cache.GetOrAdd(cacheEntry, pke => proxyBuilder.CreateProxyType(pke.BaseType, pke.Interfaces));

				var result = Activator.CreateInstance(proxyType);
				var proxy = (IProxy) result;
				proxy.Interceptor = new LiteLazyInitializer(EntityName, id, session, PersistentClass);
				return (INHibernateProxy) result;
			}
			catch (Exception ex)
			{
				log.Error("Creating a proxy instance failed", ex);
				throw new HibernateException("Creating a proxy instance failed", ex);
			}
		}

		public override object GetFieldInterceptionProxy(object instanceToWrap)
		{
			var factory = new ProxyFactory();
			var proxyType = factory.CreateProxyType(PersistentClass, typeof(IFieldInterceptorAccessor));
			var result = Activator.CreateInstance(proxyType);
			var proxy = (IProxy) result;
			proxy.Interceptor = new DefaultDynamicLazyFieldInterceptor();
			return result;
		}
	}
}
