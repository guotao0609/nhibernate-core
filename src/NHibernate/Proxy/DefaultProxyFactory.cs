using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Proxy
{
	[Serializable]
	public class DefaultProxyFactory : AbstractProxyFactory
	{
		static readonly ConcurrentDictionary<ProxyCacheEntry, Func<ILazyInitializer, IProxyFactory, INHibernateProxy>> Cache =
			new ConcurrentDictionary<ProxyCacheEntry, Func<ILazyInitializer, IProxyFactory, INHibernateProxy>>();

		protected static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof (DefaultProxyFactory));

		public DefaultProxyFactory()
		{
		}

		protected DefaultProxyFactory(SerializationInfo info, StreamingContext context)
			: base(info, context) { }

		public override INHibernateProxy GetProxy(object id, ISessionImplementor session)
		{
			try
			{
				var proxyBuilder = new NHibernateProxyBuilder(GetIdentifierMethod, SetIdentifierMethod, ComponentIdType, OverridesEquals);
				var cacheEntry = new ProxyCacheEntry(IsClassProxy ? PersistentClass : typeof(object), Interfaces);
				var proxyActivator = Cache.GetOrAdd(cacheEntry, pke => CreateProxyActivator(proxyBuilder, pke));
				return proxyActivator(new LiteLazyInitializer(EntityName, id, session, PersistentClass), this);
			}
			catch (Exception ex)
			{
				log.Error("Creating a proxy instance failed", ex);
				throw new HibernateException("Creating a proxy instance failed", ex);
			}
		}

		static Func<ILazyInitializer, IProxyFactory, INHibernateProxy> CreateProxyActivator(NHibernateProxyBuilder proxyBuilder, ProxyCacheEntry pke)
		{
			var type = proxyBuilder.CreateProxyType(pke.BaseType, pke.Interfaces);
			var ctor = type.GetConstructor(new[] {typeof(ILazyInitializer), typeof(IProxyFactory)});
			var li = Expression.Parameter(typeof(ILazyInitializer));
			var pf = Expression.Parameter(typeof(IProxyFactory));
			return Expression.Lambda<Func<ILazyInitializer, IProxyFactory, INHibernateProxy>>(Expression.New(ctor, li, pf), li, pf).Compile();
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
