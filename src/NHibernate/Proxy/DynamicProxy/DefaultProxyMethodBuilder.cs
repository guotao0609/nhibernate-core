#region Credits

// This work is based on LinFu.DynamicProxy framework (c) Philip Laureano who has donated it to NHibernate project.
// The license is the same of NHibernate license (LGPL Version 2.1, February 1999).
// The source was then modified to be the default DynamicProxy of NHibernate project.

#endregion

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NHibernate.Proxy.DynamicProxy
{
	class DefaultyProxyMethodBuilder : IProxyMethodBuilder
	{
		public DefaultyProxyMethodBuilder() : this(new DefaultMethodEmitter()) { }

		public DefaultyProxyMethodBuilder(IMethodBodyEmitter emitter)
		{
			MethodBodyEmitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
		}

		public IMethodBodyEmitter MethodBodyEmitter { get; }

		public virtual void CreateProxiedMethod(FieldInfo field, MethodInfo method, TypeBuilder typeBuilder)
		{
			var callbackMethod = ProxyMethodBuilderHelper.GenerateMethodSignature(method.Name + "_callback", method, typeBuilder);
			var proxyMethod = ProxyMethodBuilderHelper.GenerateMethodSignature(method.Name, method, typeBuilder);

			MethodBodyEmitter.EmitMethodBody(proxyMethod, callbackMethod, method, field);

			typeBuilder.DefineMethodOverride(proxyMethod, method);
		}
	}
}
