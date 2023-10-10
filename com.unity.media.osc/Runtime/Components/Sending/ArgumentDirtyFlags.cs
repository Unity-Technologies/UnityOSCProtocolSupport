using System;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The flags used to specify what has changed when determining if a new OSC Message must be sent when using
    /// <see cref="OscMessageOutput"/>.
    /// </summary>
    [Flags]
    public enum ArgumentDirtyFlags
    {
        /// <summary>
        /// The argument has not changed.
        /// </summary>
        None = 0,
        /// <summary>
        /// The data value to use for the message argument has changed.
        /// </summary>
        Value = 1 << 0,
        /// <summary>
        /// The type tags to use for the message argument changed.
        /// </summary>
        Tags = 1 << 1,
        /// <summary>
        /// The message argument has completely changed.
        /// </summary>
        All = Value | Tags,
    }
}
