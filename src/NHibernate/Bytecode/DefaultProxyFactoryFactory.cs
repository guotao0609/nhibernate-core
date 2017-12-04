using NHibernate.Proxy;

namespace NHibernate.Bytecode
{
	public class DefaultProxyFactoryFactory : IProxyFactoryFactory
	{
		internal static DefaultProxyFactoryFactory Instance = new DefaultProxyFactoryFactory();

		public IProxyFactory BuildProxyFactory()
		{
			return new DefaultProxyFactory();
		}

		public IProxyValidator ProxyValidator
		{
			get { return new DynProxyTypeValidator(); }
		}

		public bool IsInstrumented(System.Type entityClass)
		{
			return true;
		}

		public bool IsProxy(object entity)
		{
			return entity is INHibernateProxy;
		}
	}
}
