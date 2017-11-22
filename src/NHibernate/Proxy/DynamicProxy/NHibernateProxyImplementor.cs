using System.Reflection;
using System.Reflection.Emit;

namespace NHibernate.Proxy.DynamicProxy
{
	class NHibernateProxyImplementor
	{
		const MethodAttributes InterceptorMethodsAttributes = MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig |
		                                                      MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual;

		const string HibernateLazyInitializerFieldName = nameof(INHibernateProxy.HibernateLazyInitializer);

		static readonly System.Type ReturnType = typeof(ILazyInitializer);
		static readonly System.Type NHibernateProxyType = typeof(INHibernateProxy);
		static readonly PropertyInfo OriginalGetter = NHibernateProxyType.GetProperty(HibernateLazyInitializerFieldName);
		static readonly MethodInfo OriginalGetterGetMethod = OriginalGetter.GetMethod;

		public static void ImplementProxy(TypeBuilder typeBuilder, FieldInfo interceptor)
		{
			// Implement the getter
			var getMethod = typeBuilder.DefineMethod($"{NHibernateProxyType.FullName}.get_{HibernateLazyInitializerFieldName}", InterceptorMethodsAttributes, CallingConventions.HasThis, ReturnType, System.Type.EmptyTypes);
			getMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);

			var IL = getMethod.GetILGenerator();

			// This is equivalent to:
			// get { return (INHibernateProxy)__interceptor; }
			IL.Emit(OpCodes.Ldarg_0);
			IL.Emit(OpCodes.Ldfld, interceptor);
			IL.Emit(OpCodes.Castclass, ReturnType);
			IL.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(getMethod, OriginalGetterGetMethod);
			var property = typeBuilder.DefineProperty($"{NHibernateProxyType.FullName}.{HibernateLazyInitializerFieldName}", OriginalGetter.Attributes, ReturnType, System.Type.EmptyTypes);
			property.SetGetMethod(getMethod);
		}
	}
}
