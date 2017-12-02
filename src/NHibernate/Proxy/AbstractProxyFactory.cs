using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NHibernate.Engine;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Proxy
{
	/// <summary>
	/// Convenient common implementation for ProxyFactory
	/// </summary>
	public abstract class AbstractProxyFactory: IProxyFactory, ISerializable
	{

		protected virtual string EntityName { get; private set; }
		protected virtual System.Type PersistentClass { get; private set; }
		protected virtual System.Type[] Interfaces { get; private set; }
		protected virtual MethodInfo GetIdentifierMethod { get; private set; }
		protected virtual MethodInfo SetIdentifierMethod { get; private set; }
		protected virtual IAbstractComponentType ComponentIdType { get; private set; }
		protected virtual bool OverridesEquals { get; set; }

		protected bool IsClassProxy
		{
			get { return Interfaces.Length == 1; }
		}

		protected AbstractProxyFactory()
		{
		}

		protected AbstractProxyFactory(SerializationInfo info, StreamingContext context)
		{
			EntityName = (string) info.GetValue(nameof(EntityName), typeof(string));
			PersistentClass = (System.Type) info.GetValue(nameof(PersistentClass), typeof(System.Type));
			Interfaces = (System.Type[]) info.GetValue(nameof(Interfaces), typeof(System.Type[]));
			GetIdentifierMethod = (MethodInfo) info.GetValue(nameof(GetIdentifierMethod), typeof(MethodInfo));
			SetIdentifierMethod = (MethodInfo) info.GetValue(nameof(SetIdentifierMethod), typeof(MethodInfo));
			ComponentIdType = (IAbstractComponentType) info.GetValue(nameof(ComponentIdType), typeof(IAbstractComponentType));
			OverridesEquals = (bool) info.GetValue(nameof(OverridesEquals), typeof(bool));
		}

		public virtual void PostInstantiate(string entityName, System.Type persistentClass, ISet<System.Type> interfaces,
																				MethodInfo getIdentifierMethod, MethodInfo setIdentifierMethod,
																				IAbstractComponentType componentIdType)
		{
			EntityName = entityName;
			PersistentClass = persistentClass;
			Interfaces = new System.Type[interfaces.Count];

			if (interfaces.Count > 0)
			{
				interfaces.CopyTo(Interfaces, 0);
			}

			GetIdentifierMethod = getIdentifierMethod;
			SetIdentifierMethod = setIdentifierMethod;
			ComponentIdType = componentIdType;
			OverridesEquals = ReflectHelper.OverridesEquals(persistentClass);
		}


		public abstract INHibernateProxy GetProxy(object id, ISessionImplementor session);

		public virtual object GetFieldInterceptionProxy(object instanceToWrap)
		{
			throw new NotSupportedException();
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(EntityName), EntityName);
			info.AddValue(nameof(PersistentClass), PersistentClass);
			info.AddValue(nameof(Interfaces), Interfaces);
			info.AddValue(nameof(GetIdentifierMethod), GetIdentifierMethod);
			info.AddValue(nameof(SetIdentifierMethod), SetIdentifierMethod);
			info.AddValue(nameof(ComponentIdType), ComponentIdType);
			info.AddValue(nameof(OverridesEquals), OverridesEquals);
		}
	}
}
