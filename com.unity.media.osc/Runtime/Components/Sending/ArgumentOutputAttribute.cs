using System;

namespace Unity.Media.Osc
{
    /// <summary>
    /// An attribute placed on a class inheriting from <see cref="IArgumentOutput"/> to enable it
    /// to be used by <see cref="OscMessageOutput"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ArgumentOutputAttribute : Attribute
    {
        /// <summary>
        /// The type of the data to output.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// If multiple argument outputs are used for the same <see cref="Type"/>, the argument output with the greater
        /// priority is used.
        /// </summary>
        /// <remarks>
        /// Use a priority greater than zero to override the default argument outputs with custom implementations.
        /// </remarks>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Creates a new <see cref="ArgumentHandlerAttribute"/> instance.
        /// </summary>
        /// <param name="type">The type of the data to output.</param>
        public ArgumentOutputAttribute(Type type)
        {
            Type = type;
        }
    }
}
