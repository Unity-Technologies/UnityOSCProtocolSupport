using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The base class <see cref="OscMessageOutput"/> uses to handle writing arguments.
    /// </summary>
    /// <remarks>
    /// Inherit from this class and decorate the subclass with the <see cref="ArgumentOutputAttribute"/> to
    /// add support for custom types to <see cref="OscMessageOutput"/>.
    /// </remarks>
    public interface IArgumentOutput
    {
        /// <summary>
        /// The type tags this output writes.
        /// </summary>
        public TypeTag[] Tags { get; }

        /// <summary>
        /// Is the argument output able to read values from the argument source.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Sets the source of the argument values.
        /// </summary>
        /// <param name="sourceObject">The object to get the values from.</param>
        /// <param name="member">The member of <paramref name="sourceObject"/> to source the argument value from.</param>
        public void SetSource(Object sourceObject, MemberInfo member);

        /// <summary>
        /// Checks if the argument source value has changed since the last update.
        /// </summary>
        /// <returns>The dirty flags indicating how the argument has changed.</returns>
        public ArgumentDirtyFlags UpdateValue();

        /// <summary>
        /// Writes the argument data to a message.
        /// </summary>
        /// <param name="sender">The sender used to write the message.</param>
        public void Write(OscClient sender);
    }

    /// <summary>
    /// The base class for <see cref="IArgumentOutput"/> implementations.
    /// </summary>
    /// <typeparam name="T">The type of value to output.</typeparam>
    [Serializable]
    public abstract class ArgumentOutput<T> : IArgumentOutput
    {
        Object m_Source;
        Func<Object, T> m_Getter;

        /// <inheritdoc />
        public abstract TypeTag[] Tags { get; }

        /// <inheritdoc />
        public virtual bool IsValid => m_Source != null && m_Getter != null;

        /// <inheritdoc />
        public virtual void SetSource(Object sourceObject, MemberInfo member)
        {
            m_Source = sourceObject;
            m_Getter = member != null
#if ENABLE_IL2CPP
                ? m_Getter = GetReflectionGetter(member)
#else
                ? m_Getter = DynamicGetterCache<T>.GetGetter(member)
#endif
                : null;
        }

        /// <inheritdoc />
        public ArgumentDirtyFlags UpdateValue()
        {
            var value = m_Getter.Invoke(m_Source);
            return UpdateValue(value);
        }

        /// <inheritdoc />
        public abstract void Write(OscClient sender);

        /// <summary>
        /// Checks if the argument source value has changed since the last update.
        /// </summary>
        /// <param name="value">The latest value read from the source object.</param>
        /// <returns>The dirty flags indicating how the argument has changed.</returns>
        protected abstract ArgumentDirtyFlags UpdateValue(T value);

        static Func<Object, T> GetReflectionGetter(MemberInfo member)
        {
            return source =>
            {
                return member switch
                {
                    FieldInfo field => (T)field.GetValue(source),
                    PropertyInfo property => (T)property.GetValue(source),
                    MethodInfo method => (T)method.Invoke(source, null),
                    _ => throw new ArgumentOutOfRangeException(nameof(member), $"Invalid member type {member?.GetType().Name}.")
                };
            };
        }
    }

    /// <summary>
    /// The base class for arguments that output simple value types.
    /// </summary>
    /// <typeparam name="T">The struct implementing <see cref="IEquatable{T}"/> to output.</typeparam>
    [Serializable]
    public abstract class ArgumentOutputEquatable<T> : ArgumentOutput<T>
        where T : struct, IEquatable<T>
    {
        T? m_Value;

        /// <summary>
        /// The latest value of the argument to send.
        /// </summary>
        protected T Value => m_Value ?? default;

        /// <inheritdoc />
        protected override ArgumentDirtyFlags UpdateValue(T value)
        {
            if (!m_Value.HasValue || !m_Value.Value.Equals(value))
            {
                m_Value = value;
                return ArgumentDirtyFlags.Value;
            }

            return ArgumentDirtyFlags.None;
        }
    }
}
