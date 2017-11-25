using System;
using System.Reflection;
using NHibernate.Engine;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Proxy.Poco;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Proxy
{
	//Since v5.1
	[Obsolete("This class is not used anymore and will be removed in a future version. Please implement your version of ILazyInitializer")]
	[Serializable]
	public class DefaultLazyInitializer : BasicLazyInitializer, DynamicProxy.IInterceptor
	{
		public DefaultLazyInitializer(string entityName, System.Type persistentClass, object id, MethodInfo getIdentifierMethod,
							   MethodInfo setIdentifierMethod, IAbstractComponentType componentIdType,
							   ISessionImplementor session, bool overridesEquals)
			: base(entityName, persistentClass, id, getIdentifierMethod, setIdentifierMethod, componentIdType, session, overridesEquals) {}

		public object Intercept(InvocationInfo info)
		{
			object returnValue;
			try
			{
				returnValue = base.Invoke(info.TargetMethod, info.Arguments, info.Target);

				// Avoid invoking the actual implementation, if possible
				if (returnValue != InvokeImplementation)
				{
					return returnValue;
				}

				returnValue = info.TargetMethod.Invoke(GetImplementation(), info.Arguments);
			}
			catch (TargetInvocationException ex)
			{
				throw ReflectHelper.UnwrapTargetInvocationException(ex);
			}

			return returnValue;
		}
	}
}
