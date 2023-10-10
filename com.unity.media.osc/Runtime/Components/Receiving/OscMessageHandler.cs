using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// Use this component to apply data from OSC Messages to the Unity Scene using UnityEvents.
    /// </summary>
    /// <remarks>
    /// This component can handle messages containing multiple data arguments of common
    /// Unity types. The events used to apply message data are invoked before Update
    /// in the player loop.
    /// </remarks>
    [AddComponentMenu("OSC/OSC Message Handler")]
    public sealed class OscMessageHandler : OscMessageHandlerBase
    {
        [SerializeReference]
        List<ArgumentHandler> m_Arguments = new List<ArgumentHandler>();

        /// <summary>
        /// The number of arguments this message handler reads.
        /// </summary>
        public int ArgumentCount => m_Arguments.Count;

        /// <inheritdoc />
        protected override void ValueRead(OscMessage message)
        {
            var argIndex = 0;

            foreach (var argument in m_Arguments)
            {
                var argOffset = argument.Enqueue(message, argIndex);

                if (argOffset < 0)
                    break;

                argIndex += argOffset;
            }
        }

        /// <inheritdoc />
        protected override void MainThreadAction()
        {
            foreach (var argument in m_Arguments)
            {
                if (isActiveAndEnabled)
                {
                    argument.Invoke();
                }
                else
                {
                    argument.Clear();
                }
            }
        }

        /// <summary>
        /// Adds an argument handler to the end of the argument list.
        /// </summary>
        /// <param name="handler">The argument handler to add.</param>
        public void AddArgument(ArgumentHandler handler)
        {
            if (handler == null)
                return;

            m_Arguments.Add(handler);
        }

        /// <summary>
        /// Removes an argument handler from the argument list.
        /// </summary>
        /// <param name="handler">The argument handler to remove.</param>
        /// <returns><see langword="true"/> if an argument handler was removed, otherwise, <see langword="false"/>.</returns>
        public bool RemoveArgument(ArgumentHandler handler)
        {
            if (handler == null)
                return false;

            return m_Arguments.Remove(handler);
        }

        /// <summary>
        /// Gets an argument handler according to its index.
        /// </summary>
        /// <param name="index">The index of the argument handler.</param>
        /// <returns>The argument handler, or <see langword="null"/> if <paramref name="index"/> is invalid.</returns>
        public ArgumentHandler GetArgument(int index)
        {
            if (index < 0 || index >= m_Arguments.Count)
                return null;

            return m_Arguments[index];
        }
    }
}
