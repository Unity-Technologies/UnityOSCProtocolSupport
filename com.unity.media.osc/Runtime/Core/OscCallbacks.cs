using System;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class containing the actions used to handle an invoked OSC Method.
    /// </summary>
    public class OscCallbacks
    {
        /// <summary>
        /// A method executed immediately when the a message is received at the associated OSC address, on the server background thread.
        /// </summary>
        /// <remarks>
        /// All message values must be read during this callback, as the data it points to may be overwritten afterwards.
        /// </remarks>
        public readonly Action<OscMessage> ReadMessage;

        /// <summary>
        /// An optional method, which will be queued for execution on the main thread after the message was received.
        /// </summary>
        /// <remarks>
        /// This is useful for UnityEvents and anything that needs a main thread only Unity API.
        /// </remarks>
        public readonly Action MainThreadQueued;

        /// <summary>
        /// Creates a new <see cref="OscCallbacks"/> instance.
        /// </summary>
        /// <param name="readMessage">
        /// A method executed immediately when the a message is received at the associated OSC address, on the server background thread.
        /// All message values must be read during this callback, as the data it points to may be overwritten afterwards.
        /// </param>
        /// <param name="mainThreadQueued">
        /// An optional method, which will be queued for execution on the main thread in the next frame after the message was received.
        /// This is useful for UnityEvents and anything that needs a main thread only Unity API.
        /// </param>
        public OscCallbacks(Action<OscMessage> readMessage, Action mainThreadQueued = null)
        {
            ReadMessage = readMessage;
            MainThreadQueued = mainThreadQueued;
        }

        /// <summary>
        /// Combines two sets of callbacks.
        /// </summary>
        /// <param name="a">The first set of callbacks.</param>
        /// <param name="b">The second set of callbacks.</param>
        /// <returns>A new <see cref="OscCallbacks"/> instance with the combined callbacks.</returns>
        public static OscCallbacks operator +(OscCallbacks a, OscCallbacks b)
        {
            var mainThread = a.MainThreadQueued == null ? b.MainThreadQueued : a.MainThreadQueued + b.MainThreadQueued;
            var valueRead = a.ReadMessage == null ? b.ReadMessage : a.ReadMessage + b.ReadMessage;
            return new OscCallbacks(valueRead, mainThread);
        }

        /// <summary>
        /// Removes a set of callbacks.
        /// </summary>
        /// <param name="a">The set of callbacks to remove from.</param>
        /// <param name="b">The set of callbacks to remove.</param>
        /// <returns>A new <see cref="OscCallbacks"/> instance with the callbacks removed.</returns>
        public static OscCallbacks operator -(OscCallbacks a, OscCallbacks b)
        {
            var mainThread = a.MainThreadQueued == null ? b.MainThreadQueued : a.MainThreadQueued - b.MainThreadQueued;
            var valueRead = a.ReadMessage == null ? b.ReadMessage : a.ReadMessage - b.ReadMessage;
            return new OscCallbacks(valueRead, mainThread);
        }
    }
}
