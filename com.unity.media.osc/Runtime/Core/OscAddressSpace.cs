using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class that represents an OSC Address Space.
    /// </summary>
    /// <remarks>
    /// An OSC Address Space is the collection of all the OSC Addresses and their OSC Methods (callbacks)
    /// for an OSC Server. It is used to map the OSC Address Pattern in received messages to
    /// any matching callbacks. This class is thread-safe.
    /// </remarks>
    public sealed class OscAddressSpace : IDisposable
    {
        const int k_DefaultCapacity = 16;

        struct PatternData
        {
            public OscAddress Address;
            public OscCallbacks Callbacks;
        }

        readonly Dictionary<OscAddress, OscCallbacks> m_AddressToCallbacks;
        readonly List<PatternData> m_Patterns;
        readonly object m_Lock = new object();

        /// <summary>
        /// Creates a new <see cref="OscAddressSpace"/> instance.
        /// </summary>
        /// <param name="initialCapacity">The default address capacity.</param>
        public OscAddressSpace(int initialCapacity = k_DefaultCapacity)
        {
            m_AddressToCallbacks = new Dictionary<OscAddress, OscCallbacks>(initialCapacity);
            m_Patterns = new List<PatternData>(initialCapacity);
        }

        /// <summary>
        /// Disposes this instance in case it was not properly disposed.
        /// </summary>
        ~OscAddressSpace()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            Clear();
        }

        /// <summary>
        /// Gets the callbacks for any matching address or address pattern in the address space.
        /// </summary>
        /// <param name="address">The address pattern to get the callbacks for.</param>
        /// <param name="results">The list used to return the results.</param>
        public void FindMatchingCallbacks(in OscAddress address, List<OscCallbacks> results)
        {
            results.Clear();

            switch (address.Type)
            {
                case AddressType.Address:
                {
                    lock (m_Lock)
                    {
                        // check if there is a matching address
                        if (m_AddressToCallbacks.TryGetValue(address, out var callbacks))
                        {
                            results.Add(callbacks);
                        }

                        // check for any matching patterns
                        for (var i = 0; i < m_Patterns.Count; i++)
                        {
                            var pattern = m_Patterns[i];

                            if (address.Matches(pattern.Address))
                            {
                                results.Add(pattern.Callbacks);
                            }
                        }
                    }
                    break;
                }
                case AddressType.Pattern:
                {
                    lock (m_Lock)
                    {
                        // check if this pattern matches any address
                        foreach (var addressCallbacks in m_AddressToCallbacks)
                        {
                            if (address.Matches(addressCallbacks.Key))
                            {
                                results.Add(addressCallbacks.Value);
                            }
                        }

                        // check for any matching patterns
                        for (var i = 0; i < m_Patterns.Count; i++)
                        {
                            var pattern = m_Patterns[i];

                            if (address.Matches(pattern.Address))
                            {
                                results.Add(pattern.Callbacks);
                            }
                        }
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Clears all address and their registered callbacks.
        /// </summary>
        public void Clear()
        {
            lock (m_Lock)
            {
                foreach (var addressCallbacks in m_AddressToCallbacks)
                {
                    addressCallbacks.Key.Dispose();
                }

                for (var i = 0; i < m_Patterns.Count; i++)
                {
                    m_Patterns[i].Address.Dispose();
                }

                m_AddressToCallbacks.Clear();
                m_Patterns.Clear();
            }
        }

        /// <summary>
        /// Adds callbacks to the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to add the callbacks to.</param>
        /// <param name="callbacks">The callbacks to add for the address.</param>
        /// <returns><see langword="true"/> if the callbacks were successfully added; otherwise, <see langword="false"/>.</returns>
        public bool TryAddCallback(string address, OscCallbacks callbacks)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            using var parsedAddress = new OscAddress(address, Allocator.Temp);

            return TryAddCallback(parsedAddress, callbacks);
        }

        /// <summary>
        /// Adds callbacks to the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to add the callbacks to.</param>
        /// <param name="callbacks">The callbacks to add to the address.</param>
        /// <returns><see langword="true"/> if the callbacks were successfully added; otherwise, <see langword="false"/>.</returns>
        public bool TryAddCallback(in OscAddress address, OscCallbacks callbacks)
        {
            if (callbacks == null)
            {
                return false;
            }

            switch (address.Type)
            {
                case AddressType.Address:
                {
                    AddAddress(address, callbacks);
                    return true;
                }
                case AddressType.Pattern:
                {
                    AddPattern(address, callbacks);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes callbacks from the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to remove the callbacks from.</param>
        /// <param name="callbacks">The callbacks to remove from the address.</param>
        /// <returns><see langword="true"/> if there are no remaining callbacks for the address; otherwise, <see langword="false"/>.</returns>
        public bool RemoveCallback(string address, OscCallbacks callbacks)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            using var parsedAddress = new OscAddress(address, Allocator.Temp);

            return RemoveCallback(parsedAddress, callbacks);
        }

        /// <summary>
        /// Removes callbacks from the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to remove the callbacks from.</param>
        /// <param name="callbacks">The callbacks to remove from the address.</param>
        /// <returns><see langword="true"/> if there are no remaining callbacks for the address; otherwise, <see langword="false"/>.</returns>
        public bool RemoveCallback(in OscAddress address, OscCallbacks callbacks)
        {
            if (callbacks == null)
            {
                return false;
            }

            switch (address.Type)
            {
                case AddressType.Address:
                {
                    return RemoveAddressCallback(address, callbacks);
                }
                case AddressType.Pattern:
                {
                    return RemovePatternCallback(address, callbacks);
                }
            }

            return false;
        }

        /// <summary>
        /// Removes all the callbacks from the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to remove the callbacks from.</param>
        /// <returns><see langword="true"/> if any callbacks were removed; otherwise, <see langword="false"/>.</returns>
        public bool RemoveAllCallbacks(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            using var parsedAddress = new OscAddress(address, Allocator.Temp);

            return RemoveAllCallbacks(parsedAddress);
        }

        /// <summary>
        /// Removes all the callbacks from the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to remove the callbacks from.</param>
        /// <returns><see langword="true"/> if any callbacks were removed; otherwise, <see langword="false"/>.</returns>
        public bool RemoveAllCallbacks(in OscAddress address)
        {
            switch (address.Type)
            {
                case AddressType.Address:
                {
                    lock (m_Lock)
                    {
                        return RemoveAddress(address);
                    }
                }
                case AddressType.Pattern:
                {
                    lock (m_Lock)
                    {
                        if (TryGetPatternIndex(address, out var index))
                        {
                            RemovePattern(index);
                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }

        bool AddAddress(in OscAddress address, OscCallbacks callbacks)
        {
            lock (m_Lock)
            {
                // if this address is already registered add the new delegate
                if (m_AddressToCallbacks.TryGetValue(address, out var existingCallbacks))
                {
                    existingCallbacks += callbacks;
                    m_AddressToCallbacks[address] = existingCallbacks;
                    return false;
                }

                m_AddressToCallbacks.Add(new OscAddress(address, Allocator.Persistent), callbacks);
                return true;
            }
        }

        bool AddPattern(in OscAddress address, OscCallbacks callbacks)
        {
            lock (m_Lock)
            {
                // if this pattern is already registered add the new delegate
                if (TryGetPatternIndex(address, out var index))
                {
                    var pattern = m_Patterns[index];
                    pattern.Callbacks += callbacks;
                    m_Patterns[index] = pattern;
                    return false;
                }

                m_Patterns.Add(new PatternData
                {
                    Address = new OscAddress(address, Allocator.Persistent),
                    Callbacks = callbacks,
                });
                return true;
            }
        }

        bool RemoveAddressCallback(in OscAddress address, OscCallbacks callbacks)
        {
            lock (m_Lock)
            {
                if (!m_AddressToCallbacks.TryGetValue(address, out var existingCallbacks))
                {
                    return false;
                }

                existingCallbacks -= callbacks;

                if (existingCallbacks.ReadMessage == null && existingCallbacks.MainThreadQueued == null)
                {
                    RemoveAddress(address);
                    return true;
                }

                m_AddressToCallbacks[address] = existingCallbacks;
                return false;
            }
        }

        bool RemovePatternCallback(in OscAddress address, OscCallbacks callbacks)
        {
            lock (m_Lock)
            {
                if (!TryGetPatternIndex(address, out var index))
                {
                    return false;
                }

                var pattern = m_Patterns[index];
                pattern.Callbacks -= callbacks;

                if (pattern.Callbacks.ReadMessage == null && pattern.Callbacks.MainThreadQueued == null)
                {
                    RemovePattern(index);
                    return true;
                }

                m_Patterns[index] = pattern;
                return false;
            }
        }

        bool RemoveAddress(in OscAddress address)
        {
            if (m_AddressToCallbacks.ContainsKey(address))
            {
                foreach (var addressCallbacks in m_AddressToCallbacks)
                {
                    if (addressCallbacks.Key == address)
                    {
                        addressCallbacks.Key.Dispose();
                        break;
                    }
                }

                return m_AddressToCallbacks.Remove(address);
            }

            return false;
        }

        void RemovePattern(int index)
        {
            m_Patterns[index].Address.Dispose();
            m_Patterns.RemoveAt(index);
        }

        bool TryGetPatternIndex(in OscAddress address, out int index)
        {
            for (index = 0; index < m_Patterns.Count; index++)
            {
                if (m_Patterns[index].Address == address)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

