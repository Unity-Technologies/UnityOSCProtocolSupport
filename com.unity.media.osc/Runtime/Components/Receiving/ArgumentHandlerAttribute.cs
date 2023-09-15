using System;

namespace Unity.Media.Osc
{
    /// <summary>
    /// An attribute placed on a class inheriting from <see cref="ArgumentHandler"/> to enable it
    /// to be assigned from the <see cref="OscMessageHandler"/> inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ArgumentHandlerAttribute : Attribute
    {
        /// <summary>
        ///  The display name of the entry in the argument handler type selection menu.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Creates a new <see cref="ArgumentHandlerAttribute"/> instance.
        /// </summary>
        /// <param name="displayName">The display name of the entry in the argument handler type selection menu.</param>
        public ArgumentHandlerAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
