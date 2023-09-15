using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Media.Osc
{
    static class DynamicGetterCache<T>
    {
        static readonly Dictionary<MemberInfo, Func<Object, T>> s_Cache = new Dictionary<MemberInfo, Func<Object, T>>();

        public static Func<Object, T> GetGetter(MemberInfo member)
        {
            if (!s_Cache.TryGetValue(member, out var getter))
            {
                var newMethod = new DynamicMethod($"{typeof(T).FullName}.argument_get_{member.Name}", typeof(T), new[] { typeof(Object) });
                var gen = newMethod.GetILGenerator();

                switch (member)
                {
                    case FieldInfo field:
                    {
                        gen.Emit(OpCodes.Ldarg_0);
                        gen.Emit(OpCodes.Ldfld, field);
                        gen.Emit(OpCodes.Ret);
                        break;
                    }
                    case PropertyInfo property:
                    {
                        gen.Emit(OpCodes.Ldarg_0);
                        gen.Emit(OpCodes.Call, property.GetGetMethod());
                        gen.Emit(OpCodes.Ret);
                        break;
                    }
                    case MethodInfo method:
                    {
                        gen.Emit(OpCodes.Ldarg_0);
                        gen.Emit(OpCodes.Call, method);
                        gen.Emit(OpCodes.Ret);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(member), member, null);
                }

                getter = (Func<Object, T>)newMethod.CreateDelegate(typeof(Func<Object, T>));
                s_Cache.Add(member, getter);
            }

            return getter;
        }
    }
}
