using System;
using System.Runtime.Serialization;
using System.Security;

namespace NHibernate.Proxy.DynamicProxy
{
	[Serializable]
	public sealed class NHibernateProxyObjectReference : IObjectReference, ISerializable
	{
		readonly IProxyFactory _proxyFactory;
		readonly object _identifier;

		public NHibernateProxyObjectReference(IProxyFactory proxyFactory, object identifier)
		{
			_proxyFactory = proxyFactory;
			_identifier = identifier;
		}

		NHibernateProxyObjectReference(SerializationInfo info, StreamingContext context)
		{
			_proxyFactory = (IProxyFactory) info.GetValue(nameof(_proxyFactory), typeof(IProxyFactory));
			_identifier = info.GetValue(nameof(_identifier), typeof(object));
		}

		[SecurityCritical]
		public object GetRealObject(StreamingContext context)
		{
			return _proxyFactory.GetProxy(_identifier, null);
		}

		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(_proxyFactory), _proxyFactory);
			info.AddValue(nameof(_identifier), _identifier);
		}
	}
}
