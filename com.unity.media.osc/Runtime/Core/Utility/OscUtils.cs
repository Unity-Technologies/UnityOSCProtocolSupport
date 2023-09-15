using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class containing general OSC related utility methods.
    /// </summary>
    public static unsafe class OscUtils
    {
        [ThreadStatic]
        static StringBuilder s_Builder;
        [ThreadStatic]
        static byte[] s_TempBuffer;

        /// <summary>
        /// Checks if a character is valid in an OSC Address.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns><see langword="true"/> if the character is valid; otherwise, <see langword="false"/>.</returns>
        public static bool CharacterIsValidInAddress(char c)
        {
            switch (c)
            {
                case ' ':
                case '#':
                case '*':
                case '?':
                case ',':
                case '[':
                case ']':
                case '{':
                case '}':
                    return false;
                default:
                    return c.IsPrintableASCII();
            }
        }

        /// <summary>
        /// Checks if a character is valid in an OSC Address Pattern.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns><see langword="true"/> if the character is valid; otherwise, <see langword="false"/>.</returns>
        public static bool CharacterIsValidInAddressPattern(char c)
        {
            switch (c)
            {
                case ' ':
                case '#':
                    return false;
                default:
                    return c.IsPrintableASCII();
            }
        }

        /// <summary>
        /// Changes a string to make it a valid OSC Address if possible.
        /// </summary>
        /// <param name="address">The address to make valid.</param>
        /// <param name="type">The type of the address to validate.</param>
        public static void ValidateAddress(ref string address, AddressType type)
        {
            if (address == null)
                return;
            if (address.Length < OscConstants.MinAddressLength)
                return;
            if (type == AddressType.Invalid)
                return;

            s_Builder ??= new StringBuilder();
            s_Builder.Clear();

            for (var i = 0; i < address.Length; i++)
            {
                var c = address[i];

                switch (type)
                {
                    case AddressType.Address:
                    {
                        if (!CharacterIsValidInAddress(c))
                            continue;

                        // remove repeated slashes
                        if (i > 0 && c == '/' && address[i - 1] == '/')
                            continue;

                        s_Builder.Append(c);
                        break;
                    }
                    case AddressType.Pattern:
                    {
                        if (!CharacterIsValidInAddressPattern(c))
                            continue;

                        // remove groups of more than two repeated slashes
                        if (i > 1 && c == '/' && address[i - 1] == '/' && address[i - 2] == '/')
                            continue;

                        s_Builder.Append(c);
                        break;
                    }
                }
            }

            // addresses start with a slash
            if (s_Builder.Length > 0 && s_Builder[0] != '/')
            {
                s_Builder.Insert(0, '/');
            }

            // addresses do not end with a slash
            while (s_Builder.Length > 1 && s_Builder[s_Builder.Length - 1] == '/')
            {
                s_Builder.Remove(s_Builder.Length - 1, 1);
            }

            address = s_Builder.ToString();
        }

        /// <summary>
        /// Determines if a string represents a valid OSC Address or OSC Address Pattern.
        /// </summary>
        /// <param name="address">The address to classify.</param>
        /// <returns>The type of the address.</returns>
        public static AddressType GetAddressType(string address)
        {
            if (address == null)
                return AddressType.Invalid;

            // The ASCII encoding conversion replaces non-ASCII characters with '?'. Since we want to detect
            // non-ASCII characters as invalid, we use the UTF8 conversion instead and check that the byte length
            // matches the character count.
            var maxLength = Encoding.UTF8.GetMaxByteCount(address.Length);
            EnsureCapacity(ref s_TempBuffer, maxLength);
            var length = Encoding.UTF8.GetBytes(address, 0, address.Length, s_TempBuffer, 0);

            if (length != address.Length)
                return AddressType.Invalid;

            fixed (byte* ptr = s_TempBuffer)
            {
                return GetAddressType(ptr, length);
            }
        }

        /// <summary>
        /// Determines if a string represents a valid OSC Address or OSC Address Pattern.
        /// </summary>
        /// <param name="address">The buffer of the ASCII string to classify.</param>
        /// <param name="length">The length of the string in bytes.</param>
        /// <returns>The type of the address.</returns>
        public static AddressType GetAddressType(byte* address, int length)
        {
            if (address == null)
                return AddressType.Invalid;
            if (length < OscConstants.MinAddressLength)
                return AddressType.Invalid;
            if (address[0] != '/')
                return AddressType.Invalid;

            var endPtr = address + length;
            var type = AddressType.Address;
            var inBrackets = false;
            var inBraces = false;

            // skip the first character as we already check it
            for (var ptr = address + 1; ptr < endPtr; ptr++)
            {
                var c = (char)*ptr;

                switch (c)
                {
                    case ' ':
                    case '#':
                    {
                        return AddressType.Invalid;
                    }
                    case '/':
                    {
                        if (inBrackets || inBraces)
                        {
                            return AddressType.Invalid;
                        }

                        // patterns or addresses cannot end with a slash
                        if (ptr == endPtr - 1)
                        {
                            return AddressType.Invalid;
                        }

                        // if there are two slashes sequentially this cannot be an address
                        if (ptr[-1] == '/')
                        {
                            type = AddressType.Pattern;
                        }

                        break;
                    }
                    case '*':
                    case '?':
                    {
                        if (inBrackets || inBraces)
                        {
                            return AddressType.Invalid;
                        }
                        type = AddressType.Pattern;
                        break;
                    }
                    case ',':
                    {
                        if (!inBraces)
                        {
                            return AddressType.Invalid;
                        }
                        break;
                    }
                    case '[':
                    {
                        if (inBrackets || inBraces)
                        {
                            return AddressType.Invalid;
                        }
                        inBrackets = true;
                        type = AddressType.Pattern;
                        break;
                    }
                    case ']':
                    {
                        if (!inBrackets)
                        {
                            return AddressType.Invalid;
                        }
                        inBrackets = false;
                        break;
                    }
                    case '{':
                    {
                        if (inBrackets || inBraces)
                        {
                            return AddressType.Invalid;
                        }
                        inBraces = true;
                        type = AddressType.Pattern;
                        break;
                    }
                    case '}':
                    {
                        if (!inBraces)
                        {
                            return AddressType.Invalid;
                        }
                        inBraces = false;
                        break;
                    }
                    default:
                    {
                        if (!c.IsPrintableASCII())
                        {
                            return AddressType.Invalid;
                        }
                        break;
                    }
                }
            }

            // all braces must be closed by end of address
            if (inBrackets || inBraces)
            {
                return AddressType.Invalid;
            }

            return type;
        }

        /// <summary>
        /// Creates an OSC Message tag string from a <see cref="TypeTag"/> array.
        /// </summary>
        /// <param name="tags">The tags to include in the string.</param>
        /// <param name="allocator">The allocator to use for the string.</param>
        /// <returns>A new <see cref="NativeArray{T}"/> containing the string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tags"/> is null.</exception>
        public static NativeArray<byte> CreateTagString(TypeTag[] tags, Allocator allocator = Allocator.Persistent)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            var tagString = new NativeArray<byte>(tags.Length + 1, allocator, NativeArrayOptions.UninitializedMemory)
            {
                [0] = (byte)',',
            };
            for (var i = 0; i < tags.Length; i++)
            {
                tagString[i + 1] = (byte)tags[i];
            }
            return tagString;
        }

        /// <summary>
        /// Pins a managed array in memory so an unsafe pointer may be used to index it.
        /// </summary>
        /// <param name="array">The array to pin.</param>
        /// <param name="handle">Returns the handle of the pinned array.</param>
        /// <typeparam name="TData">The type of data in the array.</typeparam>
        /// <typeparam name="TPtr">The type of the returned pointer.</typeparam>
        /// <returns>The pointer to the start of the pinned array.</returns>
        internal static unsafe TPtr* PinPtr<TData, TPtr>(TData[] array, out GCHandle handle)
            where TData : unmanaged
            where TPtr : unmanaged
        {
            handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            return (TPtr*)handle.AddrOfPinnedObject();
        }

        /// <summary>
        /// Resizes an array if needed so it can contain a specified number of elements.
        /// </summary>
        /// <param name="array">The array to resize.</param>
        /// <param name="requiredCapacity">The minimum length of the resized array.</param>
        /// <returns><see langword="true"/> if the array was resized; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EnsureCapacity<T>(ref T[] array, int requiredCapacity)
        {
            if (array == null)
            {
                array = new T[requiredCapacity];
                return true;
            }

            if (array.Length < requiredCapacity)
            {
                Array.Resize(ref array, Math.Max(requiredCapacity, 2 * array.Length));
                return true;
            }

            return false;
        }
    }
}
