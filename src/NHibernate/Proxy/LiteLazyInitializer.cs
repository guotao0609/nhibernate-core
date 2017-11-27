using System;
using NHibernate.Engine;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Proxy
{
	[Serializable]
	class LiteLazyInitializer : AbstractLazyInitializer, DynamicProxy.IInterceptor
	{
		internal LiteLazyInitializer(string entityName, object id, ISessionImplementor session, System.Type persistentClass) 
			: base(entityName, id, session)
		{
			PersistentClass = persistentClass;
		}

		public override System.Type PersistentClass { get; }

		public object Intercept(InvocationInfo info)
		{
			throw new System.NotImplementedException();
		}
	}
}
