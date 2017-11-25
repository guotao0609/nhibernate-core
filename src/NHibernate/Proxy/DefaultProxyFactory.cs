using System;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Proxy
{
	public class DefaultProxyFactory : AbstractProxyFactory
	{

		protected static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof (DefaultProxyFactory));

		public override INHibernateProxy GetProxy(object id, ISessionImplementor session)
		{
			var factory = new ProxyFactory(new NHibernateProxyMethodBuilder(GetIdentifierMethod, SetIdentifierMethod, ComponentIdType, OverridesEquals));
			try
			{
				var proxyType = factory.CreateProxyType(IsClassProxy ? PersistentClass : typeof(object), Interfaces);

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
