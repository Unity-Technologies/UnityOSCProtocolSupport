using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Media.Osc
{
    /// <summary>
    /// Use this class to manage the event callbacks for an argument in an OSC Message.
    /// </summary>
    [Serializable]
    public abstract class ArgumentHandler
    {
        /// <summary>
        /// The number of consecutive type tags in the OSC Message this handler reads.
        /// </summary>
        protected abstract int ArgumentCount { get; }

        /// <summary>
        /// Clears all queued arguments.
        /// </summary>
        internal abstract void Clear();

        /// <summary>
        /// Reads the argument data from a message and queues an invocation of the callbacks.
        /// </summary>
        /// <remarks>
        /// This may be called from thread pool and must only use thread-safe APIs.
        /// </remarks>
        /// <param name="message">The message to read the argument from.</param>
        /// <param name="argumentIndex">The argument index in the message to read at.</param>
        /// <returns>The number of message arguments read, or -1 if <see cref="ArgumentCount"/> exceeds the
        /// number of remaining message arguments.</returns>
        internal abstract int Enqueue(OscMessage message, int argumentIndex);

        /// <summary>
        /// Invokes the callbacks for all queued arguments.
        /// </summary>
        internal abstract void Invoke();
    }

    /// <summary>
    /// A <see cref="ArgumentHandler"/> for OSC Message arguments that have no data.
    /// </summary>
    [Serializable]
    [ArgumentHandler("Void")]
    public sealed class ArgumentHandlerVoid : ArgumentHandler
    {
        [Serializable]
        sealed class Event : UnityEvent
        {
        }

        [SerializeField, Tooltip("The callbacks executed when an OSC Message is received.")]
        Event m_Event;

        int m_Count = 0;

        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <summary>
        /// Subscribes to the event triggered when this argument is received.
        /// </summary>
        /// <param name="action">The event listener to add.</param>
        public void AddListener(UnityAction action)
        {
            m_Event.AddListener(action);
        }

        /// <summary>
        /// Unsubscribes from the event triggered when this argument is received.
        /// </summary>
        /// <param name="action">The event listener to remove.</param>
        public void RemoveListener(UnityAction action)
        {
            m_Event.RemoveListener(action);
        }

        /// <inheritdoc/>
        internal override void Clear()
        {
            Interlocked.Exchange(ref m_Count, 0);
        }

        /// <inheritdoc/>
        internal override int Enqueue(OscMessage message, int argumentIndex)
        {
            Interlocked.Increment(ref m_Count);
            return ArgumentCount;
        }

        /// <inheritdoc/>
        internal override void Invoke()
        {
            while (m_Count > 0)
            {
                m_Event.Invoke();
                Interlocked.Decrement(ref m_Count);
            }
        }
    }

    /// <summary>
    /// A <see cref="ArgumentHandler"/> for OSC Message arguments that have data.
    /// </summary>
    /// <remarks>
    /// To add the ability to read custom data types from OSC Messages, inherit from this class
    /// and decorate it with the <see cref="ArgumentHandlerAttribute"/>.
    /// </remarks>
    /// <typeparam name="T">The type of data in the message argument.</typeparam>
    [Serializable]
    public abstract class ArgumentHandler<T> : ArgumentHandler
    {
        [Serializable]
        sealed class Event : UnityEvent<T>
        {
        }

        [SerializeField, Tooltip("The callbacks executed when an OSC Message is received.")]
        Event m_Event;

        readonly ConcurrentQueue<T> m_Actions = new ConcurrentQueue<T>();

        /// <summary>
        /// Subscribes to the event triggered when this argument is received.
        /// </summary>
        /// <param name="action">The event listener to add.</param>
        public void AddListener(UnityAction<T> action)
        {
            m_Event.AddListener(action);
        }

        /// <summary>
        /// Unsubscribes from the event triggered when this argument is received.
        /// </summary>
        /// <param name="action">The event listener to remove.</param>
        public void RemoveListener(UnityAction<T> action)
        {
            m_Event.RemoveListener(action);
        }

        /// <inheritdoc/>
        internal override void Clear()
        {
            while (m_Actions.TryDequeue(out _))
            {
            }
        }

        /// <inheritdoc/>
        internal override int Enqueue(OscMessage message, int argumentIndex)
        {
            // ensure the message contains enough arguments to read
            if (argumentIndex + ArgumentCount > message.ArgumentCount)
            {
                return -1;
            }

            try
            {
                if (TryReadArgument(message, argumentIndex, out var value))
                {
                    m_Actions.Enqueue(value);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return ArgumentCount;
        }

        /// <inheritdoc/>
        internal override void Invoke()
        {
            while (m_Actions.TryDequeue(out var value))
            {
                m_Event.Invoke(value);
            }
        }

        /// <summary>
        /// Reads the argument data from an OSC Message.
        /// </summary>
        /// <remarks>
        /// This may be called from thread pool and must only use thread-safe APIs.
        /// </remarks>
        /// <param name="message">The message to read the argument from.</param>
        /// <param name="argumentIndex">The argument index in the message to read at.</param>>
        /// <param name="value">The read argument value.</param>
        /// <returns><see langword="true"/> if the argument was successfully read, otherwise, <see langword="false"/>.</returns>
        protected abstract bool TryReadArgument(OscMessage message, int argumentIndex, out T value);
    }
}
