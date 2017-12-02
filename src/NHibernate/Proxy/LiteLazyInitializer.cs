using System;
using NHibernate.Engine;

namespace NHibernate.Proxy
{
	[Serializable]
	class LiteLazyInitializer : AbstractLazyInitializer
	{
		internal LiteLazyInitializer(string entityName, object id, ISessionImplementor session, System.Type persistentClass) 
			: base(entityName, id, session)
		{
			PersistentClass = persistentClass;
		}

		public override System.Type PersistentClass { get; }
	}
}