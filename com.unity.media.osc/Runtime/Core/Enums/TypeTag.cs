using System.Runtime.CompilerServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// An enum defining the tags which may be used in the OSC type tag string.
    /// </summary>
    /// <remarks>
    /// The integral value of each tag corresponds to the tag's ASCII character.
    /// </remarks>
    /// <seealso href="http://opensoundcontrol.org/spec-1_0.html"/>
    public enum TypeTag : byte
    {
        /// <summary>
        /// The "F" tag.
        /// </summary>
        /// <remarks>
        /// This is used for more compact boolean values.
        /// No bytes are allocated in the argument data.
        /// This is a non-standard tag.
        /// </remarks>
        False = 70,

        /// <summary>
        /// The "I" tag.
        /// </summary>
        /// <remarks>
        /// Alternatively called "Impulse".
        /// This is used for event triggers.
        /// No bytes are allocated in the argument data.
        /// This is a non-standard tag.
        /// </remarks>
        Infinitum = 73,

        /// <summary>
        /// The "N" tag.
        /// </summary>
        /// <remarks>
        /// Alternatively named "Null", "None".
        /// No bytes are allocated in the argument data.
        /// This is a non-standard tag.
        /// </remarks>
        Nil = 78,

        /// <summary>
        /// The "S" tag.
        /// </summary>
        /// <remarks>
        /// This is used for alternate types represented as an OSC String (for example,
        /// for systems that differentiate “symbols” from “strings”).
        /// This is a non-standard tag.
        /// </remarks>
        AltTypeString = 83,

        /// <summary>
        /// The "T" tag.
        /// </summary>
        /// <remarks>
        /// This is used for more compact boolean values.
        /// No bytes are allocated in the argument data.
        /// This is a non-standard tag.
        /// </remarks>
        True = 84,

        /// <summary>
        /// The "[" tag.
        /// </summary>
        /// <remarks>
        /// This indicates the beginning of an array.
        /// The tags following are for data in the Array until a close brace tag is reached.
        /// No bytes are allocated in the argument data.
        /// This is a non-standard tag.
        /// </remarks>
        ArrayStart = 91,

        /// <summary>
        /// The "]" tag.
        /// </summary>
        /// <remarks>
        /// This indicates the end of an array.
        /// No bytes are allocated in the argument data.
        /// This is a non-standard tag.
        /// </remarks>
        ArrayEnd = 93,

        /// <summary>
        /// The "b" tag.
        /// </summary>
        /// <remarks>
        /// This is used for arbitrary data.
        /// This is a standard tag.
        /// </remarks>
        Blob = 98,

        /// <summary>
        /// The "c" tag.
        /// </summary>
        /// <remarks>
        /// This is a non-standard tag.
        /// </remarks>
        AsciiChar32 = 99,

        /// <summary>
        /// The "d" tag.
        /// </summary>
        /// <remarks>
        /// This is a non-standard tag.
        /// </remarks>
        Float64 = 100,

        /// <summary>
        /// The "f" tag.
        /// </summary>
        /// <remarks>
        /// This is a standard tag.
        /// </remarks>
        Float32 = 102,

        /// <summary>
        /// The "h" tag.
        /// </summary>
        /// <remarks>
        /// This is a non-standard tag.
        /// </remarks>
        Int64 = 104,

        /// <summary>
        /// The "i" tag.
        /// </summary>
        /// <remarks>
        /// This is a standard tag.
        /// </remarks>
        Int32 = 105,

        /// <summary>
        /// The "m" tag.
        /// </summary>
        /// <remarks>
        /// This is a non-standard tag.
        /// </remarks>
        MIDI = 109,

        /// <summary>
        /// The "r" tag.
        /// </summary>
        /// <remarks>
        /// This is a non-standard tag.
        /// </remarks>
        Color32 = 114,

        /// <summary>
        /// The "s" tag.
        /// </summary>
        /// <remarks>
        /// This is a standard tag.
        /// </remarks>
        String = 115,

        /// <summary>
        /// The "t" tag.
        /// </summary>
        /// <remarks>
        /// This is a non-standard tag.
        /// </remarks>
        TimeTag = 116,
    }

    /// <summary>
    /// A class containing extension methods for <see cref="TypeTag"/>.
    /// </summary>
    public static class TypeTagExtensions
    {
        /// <summary>
        /// Checks if a given type tag is supported by this OSC implementation.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns><see langword="true"/> if the tag is supported; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSupported(this TypeTag tag)
        {
            switch (tag)
            {
                case TypeTag.False:
                case TypeTag.Infinitum:
                case TypeTag.Nil:
                case TypeTag.AltTypeString:
                case TypeTag.True:
                case TypeTag.ArrayStart:
                case TypeTag.ArrayEnd:
                case TypeTag.Blob:
                case TypeTag.AsciiChar32:
                case TypeTag.Float64:
                case TypeTag.Float32:
                case TypeTag.Int64:
                case TypeTag.Int32:
                case TypeTag.MIDI:
                case TypeTag.Color32:
                case TypeTag.String:
                case TypeTag.TimeTag:
                    return true;
            }

            return false;
        }
    }
}

