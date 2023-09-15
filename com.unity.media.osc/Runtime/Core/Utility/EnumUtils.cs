using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.Media.Osc
{
    static class EnumUtils
    {
        static class DisplayNameCache<T> where T : Enum
        {
            public static readonly Dictionary<T, string> s_Cache = (Enum.GetValues(typeof(T)) as T[])
                .ToDictionary(value => value, value =>
                {
                    var name = value.ToString();
                    return typeof(T).GetMember(name).FirstOrDefault()?.GetCustomAttribute<InspectorNameAttribute>()?.displayName ?? name;
                });
        }

        /// <summary>
        /// Gets the display name of an enum.
        /// </summary>
        /// <remarks>
        /// This returns the name given by <see cref="InspectorNameAttribute"/> if the attribute is used.
        /// The name values are cached, so there is little cost to getting the name.
        /// </remarks>
        /// <param name="value">The enum value to get the display name of.</param>
        /// <typeparam name="T">The type of enum.</typeparam>
        /// <returns>The display name.</returns>
        public static string GetDisplayName<T>(this T value) where T : Enum
        {
            return DisplayNameCache<T>.s_Cache[value];
        }
    }
}
