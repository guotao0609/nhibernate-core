using System.Reflection;
using System.Reflection.Emit;
using NHibernate.Type;

namespace NHibernate.Proxy.DynamicProxy
{
	class NHibernateProxyMethodBuilder : IProxyMethodBuilder
	{
		const MethodAttributes InterceptorMethodsAttributes = MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig |
		                                                      MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual;

		const string HibernateLazyInitializerFieldName = nameof(INHibernateProxy.HibernateLazyInitializer);
		static readonly System.Type NHibernateProxyType = typeof(INHibernateProxy);
		static readonly PropertyInfo NHibernateProxyTypeLazyInitializerProperty = NHibernateProxyType.GetProperty(HibernateLazyInitializerFieldName);
		static readonly System.Type LazyInitializerType = typeof(ILazyInitializer);
		static readonly PropertyInfo LazyInitializerIdentifierProperty = LazyInitializerType.GetProperty(nameof(ILazyInitializer.Identifier));
		static readonly MethodInfo LazyInitializerInitializeMethod = LazyInitializerType.GetMethod(nameof(ILazyInitializer.Initialize));
		static readonly MethodInfo LazyInitializerGetImplementationMethod = LazyInitializerType.GetMethod(nameof(ILazyInitializer.GetImplementation), System.Type.EmptyTypes);
		readonly MethodInfo _getIdentifierMethod;
		readonly MethodInfo _setIdentifierMethod;
		readonly IAbstractComponentType _componentIdType;
		readonly bool _overridesEquals;

		public NHibernateProxyMethodBuilder(MethodInfo getIdentifierMethod, MethodInfo setIdentifierMethod, IAbstractComponentType componentIdType, bool overridesEquals)
		{
			_getIdentifierMethod = getIdentifierMethod;
			_setIdentifierMethod = setIdentifierMethod;
			_componentIdType = componentIdType;
			_overridesEquals = overridesEquals;
		}

		public void CreateProxiedMethod(FieldInfo field, MethodInfo method, TypeBuilder typeBuilder)
		{
			if (method == NHibernateProxyTypeLazyInitializerProperty.GetMethod)
			{
				ImplementGetLazyInitializer(typeBuilder, field);
			}
			else if (method == _getIdentifierMethod)
			{
				ImplementGetIdentifier(typeBuilder, method);
			}
			else if (method == _setIdentifierMethod)
			{
				ImplementSetIdentifier(typeBuilder, method);
			}
			else if (!_overridesEquals && method.Name == "Equals" && method.GetBaseDefinition() == typeof(object).GetMethod("Equals", new[] {typeof(object)}))
			{
//skip
			}
			else if (!_overridesEquals && method.Name == "GetHashCode" && method.GetBaseDefinition() == typeof(object).GetMethod("GetHashCode"))
			{
//skip
			}
			else if (_componentIdType != null && _componentIdType.IsMethodOf(method))
			{
				ImplementCallMethodOnEmbeddedComponentId(typeBuilder, method);
			}
			else
			{
				ImplementCallMethodOnImplementation(typeBuilder, method);
			}
		}

		static void ImplementGetLazyInitializer(TypeBuilder typeBuilder, FieldInfo interceptor)
		{
			// Implement the getter
			var getMethod = typeBuilder.DefineMethod($"{NHibernateProxyType.FullName}.get_{HibernateLazyInitializerFieldName}", InterceptorMethodsAttributes, CallingConventions.HasThis, LazyInitializerType, System.Type.EmptyTypes);
			getMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.IL);

			var IL = getMethod.GetILGenerator();

			// This is equivalent to:
			// get { return (ILazyInitializer)__interceptor; }
			IL.Emit(OpCodes.Ldarg_0);
			IL.Emit(OpCodes.Ldfld, interceptor);
			IL.Emit(OpCodes.Castclass, LazyInitializerType);
			IL.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(getMethod, NHibernateProxyTypeLazyInitializerProperty.GetMethod);

			var property = typeBuilder.DefineProperty($"{NHibernateProxyType.FullName}.{HibernateLazyInitializerFieldName}", NHibernateProxyTypeLazyInitializerProperty.Attributes, LazyInitializerType, System.Type.EmptyTypes);
			property.SetGetMethod(getMethod);
		}

		static void ImplementGetIdentifier(TypeBuilder typeBuilder, MethodInfo method)
		{
			// get => return (ReturnType)((INHibernateProxy)this).LazyInitializer.Identifier;
			var methodOverride = ProxyMethodBuilderHelper.GenerateMethodSignature(method.Name, method, typeBuilder);

			var IL = methodOverride.GetILGenerator();

			EmitCallBaseIfLazyInitializerIsNull(method, IL);

			EmitGetLazyInitializer(IL);
			IL.Emit(OpCodes.Callvirt, LazyInitializerIdentifierProperty.GetMethod);
			IL.Emit(OpCodes.Unbox_Any, method.ReturnType);
			IL.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(methodOverride, method);
		}

		static void ImplementSetIdentifier(TypeBuilder typeBuilder, MethodInfo method)
		{
			/*
			 set 
			 {
				((INHibernateLazyInitializer)this).LazyInitializer.Initialize();
				((INHibernateLazyInitializer)this).LazyInitializer.Identifier = value;
				((INHibernateLazyInitializer)this).LazyInitializer.GetImplementation().<Identifier> = value;
			 }
			 */
			var propertyType = method.GetParameters()[0].ParameterType;
			var methodOverride = ProxyMethodBuilderHelper.GenerateMethodSignature(method.Name, method, typeBuilder);
			var IL = methodOverride.GetILGenerator();

			EmitCallBaseIfLazyInitializerIsNull(method, IL);

			EmitGetLazyInitializer(IL);
			IL.Emit(OpCodes.Callvirt, LazyInitializerInitializeMethod);

			EmitGetLazyInitializer(IL);
			IL.Emit(OpCodes.Ldarg_1);
			if (propertyType.IsValueType)
				IL.Emit(OpCodes.Box, propertyType);
			IL.Emit(OpCodes.Callvirt, LazyInitializerIdentifierProperty.SetMethod);

			EmitCallImplementation(method, IL);

			IL.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(methodOverride, method);
		}

		static void ImplementCallMethodOnEmbeddedComponentId(TypeBuilder typeBuilder, MethodInfo method)
		{
			// ((INHibernateProxy)this).LazyInitializer.Identifier.<Method>(args..);
			var methodOverride = ProxyMethodBuilderHelper.GenerateMethodSignature(method.Name, method, typeBuilder);

			var IL = methodOverride.GetILGenerator();

			EmitCallBaseIfLazyInitializerIsNull(method, IL);

			EmitGetLazyInitializer(IL);
			IL.Emit(OpCodes.Callvirt, LazyInitializerIdentifierProperty.GetMethod);
			IL.Emit(OpCodes.Unbox_Any, method.DeclaringType);
			EmitCallMethod(method, IL, OpCodes.Callvirt);
			IL.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(methodOverride, method);
		}

		static void ImplementCallMethodOnImplementation(TypeBuilder typeBuilder, MethodInfo method)
		{
			/*
				return ((INHibernateProxy)this).LazyInitializer.GetImplementation().<Method>(args..);
			*/
			var methodOverride = ProxyMethodBuilderHelper.GenerateMethodSignature(method.Name, method, typeBuilder);

			var IL = methodOverride.GetILGenerator();

			EmitCallBaseIfLazyInitializerIsNull(method, IL);

			EmitCallImplementation(method, IL);
			IL.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(methodOverride, method);
		}

		static void EmitCallBaseIfLazyInitializerIsNull(MethodInfo method, ILGenerator IL)
		{
			//if (((INHibernateProxy) this).LazyInitializer == null)
			//	return base..< Method > (args..)

			EmitGetLazyInitializer(IL);
			var skipBaseCall = IL.DefineLabel();

			IL.Emit(OpCodes.Ldnull);
			IL.Emit(OpCodes.Bne_Un, skipBaseCall);

			IL.Emit(OpCodes.Ldarg_0);
			EmitCallMethod(method, IL, OpCodes.Call);
			IL.Emit(OpCodes.Ret);

			IL.MarkLabel(skipBaseCall);
		}

		static void EmitGetLazyInitializer(ILGenerator IL)
		{
			IL.Emit(OpCodes.Ldarg_0);
			IL.Emit(OpCodes.Castclass, NHibernateProxyType);
			IL.Emit(OpCodes.Callvirt, NHibernateProxyTypeLazyInitializerProperty.GetMethod);
		}

		static void EmitCallMethod(MethodInfo method, ILGenerator IL, OpCode opCode)
		{
			for (int i = 0; i < method.GetParameters().Length; i++)
				IL.Emit(OpCodes.Ldarg_S, (sbyte) (1 + i));
			IL.Emit(opCode, method);
		}

		static void EmitCallImplementation(MethodInfo method, ILGenerator IL)
		{
			//((INHibernateLazyInitializer)this).LazyInitializer.GetImplementation().<Method>(args..);
			EmitGetLazyInitializer(IL);
			IL.Emit(OpCodes.Callvirt, LazyInitializerGetImplementationMethod);
			IL.Emit(OpCodes.Unbox_Any, method.DeclaringType);
			EmitCallMethod(method, IL, OpCodes.Callvirt);
		}
	}
}
