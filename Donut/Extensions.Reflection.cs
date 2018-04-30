using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Donut
{
    public static partial class Extensions
    {
        public static IEnumerable<PropertyInfo> GetInstanceProperties(this Type type)
        { 
            return type.GetRuntimeProperties().Where(p => !p.IsStatic());
        }
        public static IEnumerable<PropertyInfo> GetPropertiesInHierarchy(this Type type, string name)
        {
            do
            {
                var typeInfo = type.GetTypeInfo();
                var propertyInfo = typeInfo.GetDeclaredProperty(name);
                if (propertyInfo != null
                    && !(propertyInfo.GetMethod ?? propertyInfo.SetMethod).IsStatic)
                {
                    yield return propertyInfo;
                }
                type = typeInfo.BaseType;
            }
            while (type != null);
        }

        public static bool IsValidStructuralType(this Type type)
        { 
            return !(type.IsGenericType()
                     || type.IsValueType()
                     || type.IsPrimitive()
                     || type.IsInterface()
                     || type.IsArray
                     || type == typeof(string) 
                   && type.IsValidStructuralPropertyType());
        }
        public static Type UnwrapNullableType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;

        public static PropertyInfo FindGetterProperty(this PropertyInfo propertyInfo)
            => propertyInfo.DeclaringType
                .GetPropertiesInHierarchy(propertyInfo.Name)
                .FirstOrDefault(p => p.GetMethod != null);

        public static PropertyInfo FindSetterProperty(this PropertyInfo propertyInfo)
            => propertyInfo.DeclaringType
                .GetPropertiesInHierarchy(propertyInfo.Name)
                .FirstOrDefault(p => p.SetMethod != null);

        public static bool IsNullableType(this Type type)
        {
            var typeInfo = type.GetTypeInfo();

            return !typeInfo.IsValueType
                   || typeInfo.IsGenericType
                   && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        public static bool IsValidStructuralPropertyType(this Type type)
        { 
            return !(type.IsGenericTypeDefinition()
                     || type.IsPointer
                     || type == typeof(object));
        }

        public static bool IsClass(this Type type)
        { 
#if NET40
            return type.IsClass;
#else
            return type.GetTypeInfo().IsClass;
#endif
        }

        public static bool IsGenericTypeDefinition(this Type type)
        { 
#if NET40
            return type.IsGenericTypeDefinition;
#else
            return type.GetTypeInfo().IsGenericTypeDefinition;
#endif
        }

        public static bool IsGenericType(this Type type)
        {  
#if NET40
            return type.IsGenericType;
#else
            return type.GetTypeInfo().IsGenericType;
#endif
        }

        public static bool IsInterface(this Type type)
        {
#if NET40
            return type.IsInterface;
#else
            return type.GetTypeInfo().IsInterface;
#endif
        }

        public static bool IsPrimitive(this Type type)
        { 
#if NET40
            return type.IsPrimitive;
#else
            return type.GetTypeInfo().IsPrimitive;
#endif
        }

        public static bool IsValueType(this Type type)
        { 
#if NET40
            return type.IsValueType;
#else
            return type.GetTypeInfo().IsValueType;
#endif
        }

        public static MethodInfo Getter(this PropertyInfo property)
        { 
#if NET40
            return property.GetGetMethod(nonPublic: true);
#else
            return property.GetMethod;
#endif
        }

        public static MethodInfo Setter(this PropertyInfo property)
        { 
#if NET40
            return property.GetSetMethod(nonPublic: true);
#else
            return property.SetMethod;
#endif
        }

        public static bool IsStatic(this PropertyInfo property)
        { 
            return (property.Getter() ?? property.Setter()).IsStatic;
        }

        public static bool IsPrimitiveConvertable(this Type value)
        {
            return value.IsPrimitive || (new[] { "String" }.Contains(value.Name));
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            // TODO: Argument validation
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns> 
        public static Type LoadType(this Type type)
        { 
            string bType = type.GetType().Name;
            if (bType != "ReflectionOnlyType")
                return type;
            Assembly asm = type.Assembly;
            asm = Assembly.Load(asm.FullName);
            return asm?.GetType(type.FullName);
        }


        /// <summary>
        /// Makes a generic type resembling type(of s)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="s"></param>
        /// <returns></returns> 
        public static object MakeGenericType(this Type type, Type s)
        {
            return type.MakeGenericType(s);
        }

		 
    }
}
