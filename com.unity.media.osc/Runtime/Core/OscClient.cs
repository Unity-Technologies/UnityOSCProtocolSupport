using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class that represents an OSC Client.
    /// </summary>
    /// <remarks>
    /// OSC Clients are responsible for creating and sending messages.
    /// This class is not thread-safe, but may be used on any thread.
    /// </remarks>
    public abstract unsafe class OscClient : IDisposable
    {
        /// <summary>
        /// An event invoked when any OSC Client sends an OSC Message.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The event may be executed from the thread pool so you must only use thread-safe APIs.
        /// When there are any subscribers to this event extra processing is done to parse the messages,
        /// so it is recommended to only use this event for debugging purposes.
        /// </para>
        /// <para>
        /// * The first parameter is the client that sent the message.<br/>
        /// * The second parameter is the parsed message instance. The message reference is only valid for the duration of the callback scope.<br/>
        /// * The third parameter is a string describing the message destination.<br/>
        /// </para>
        /// </remarks>
        public static event Action<OscClient, OscMessage, string> MessageSent;

        /// <summary>
        /// Indicates whether there are any subscribers to the <see cref="MessageSent"/> event.
        /// </summary>
        protected static bool IsMonitorCallbackRegistered => MessageSent != null;

        internal static List<OscClient> Clients { get; } = new List<OscClient>();
        internal static event Action ClientsChanged;

        enum Type
        {
            Message,
            Bundle,
        }

        struct ElementState
        {
            public Type Type;
            public OscWriter Writer;
        }

        readonly OscPacket m_Packet;
        volatile bool m_Disposed;
        bool m_AutoBundle;
        int m_AutoBundleThreshold;

        // Use a stack to keep track of what type of elements are being currently written. The stack
        // is needed to support nesting bundles. We size the array large enough to fit any reasonable
        // amount of nesting.
        ElementState[] m_ElementStack = new ElementState[64];
        int m_ElementDepth = -1;

        /// <summary>
        /// Has this instance been disposed.
        /// </summary>
        public bool IsDisposed => m_Disposed;

        /// <summary>
        /// Gets if an OSC Message or OSC Bundle is currently being constructed.
        /// </summary>
        /// <remarks>
        /// This returns <see langword="true"/> after the first call to <see cref="BeginMessage(OscAddress,string)"/>
        /// or <see cref="BeginBundle()"/> until the matching call to <see cref="EndMessage"/> or <see cref="EndBundle"/>;
        /// otherwise, <see langword="false"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public bool IsWriting
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscClient));

                return m_ElementDepth >= (m_AutoBundle ? 1 : 0);
            }
        }

        /// <summary>
        /// Indicates whether the client automatically groups messages into OSC Bundles.
        /// </summary>
        /// <remarks>
        /// When this option is enabled, OSC Messages and OSC Bundles are not sent immediately. Instead, they are nested into a single
        /// OSC Bundle that is sent once its size exceeds <see cref="AutoBundleThreshold"/> or <see cref="SendAutoBundle"/> is called.
        /// This is useful for grouping all the messages for a frame into a single packet, rather then sending each message separately.
        /// This primarily helps to reduce network overhead at the cost of a small amount of latency. The receiving device or application
        /// must support OSC Bundles.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsWriting"/> is <see langword="true"/>.</exception>
        /// <seealso cref="AutoBundleThreshold"/>
        /// <seealso cref="SendAutoBundle"/>
        public bool AutoBundle
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscClient));

                return m_AutoBundle;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscClient));
                if (IsWriting)
                    throw new InvalidOperationException($"Cannot set {nameof(AutoBundle)} until finished writing the current {m_ElementStack[m_ElementDepth].Type}.");

                if (m_AutoBundle != value)
                {
                    if (m_AutoBundle)
                    {
                        EndBundleInternal();
                    }

                    m_AutoBundle = value;

                    if (m_AutoBundle)
                    {
                        BeginBundle();
                    }
                }
            }
        }

        /// <summary>
        /// When an automatically generated OSC Bundle exceeds this size in bytes, it is sent and a new bundle is started.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="AutoBundle"/> is <see langword="true"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <seealso cref="AutoBundle"/>
        public int AutoBundleThreshold
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscClient));

                return m_AutoBundleThreshold;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscClient));

                m_AutoBundleThreshold = value;
            }
        }

        /// <summary>
        /// Indicates whether the client is able send messages.
        /// </summary>
        /// <remarks>
        /// When <see langword="false"/>, completed messages will be dropped instead of being sent.
        /// </remarks>
        public virtual bool IsReady => !IsDisposed;

        /// <summary>
        /// The size of the buffer used to write and send messages.
        /// </summary>
        protected int BufferSize => m_Packet.Buffer.Length;

        /// <summary>
        /// A description of the OSC Message destination.
        /// </summary>
        /// <remarks>
        /// Set this value to provide the message destination to any registered monitoring callbacks.
        /// </remarks>
        protected virtual string Destination { get; } = null;

        /// <summary>
        /// Creates a new <see cref="OscClient"/> instance.
        /// </summary>
        /// <param name="bufferSize">The size of the buffer used to write and send messages. Must be large enough
        /// to fit the OSC Messages to send, otherwise the messages cannot be sent.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is not larger than zero.</exception>
        protected OscClient(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Must be larger than zero.");

            m_Packet = new OscPacket(new byte[bufferSize]);

            AutoBundle = false;
            AutoBundleThreshold = Math.Min(bufferSize / 2, 1024);

            Clients.Add(this);
            ClientsChanged?.Invoke();

#if UNITY_EDITOR
            EditorApplication.quitting += Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#else
            Application.quitting += Dispose;
#endif
        }

        /// <summary>
        /// Disposes this instance in case it was not properly disposed.
        /// </summary>
        ~OscClient()
        {
            Debug.LogAssertion($"An instance of type \"{GetType().FullName}\" was not disposed!");
            Dispose(false);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            OnDispose(disposing);

            m_Packet?.Dispose();

            if (Clients.Remove(this))
            {
                ClientsChanged?.Invoke();
            }

#if UNITY_EDITOR
            EditorApplication.quitting -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
#else
            Application.quitting -= Dispose;
#endif

            m_Disposed = true;
        }

        /// <summary>
        /// Cleans up resources held by this instance.
        /// </summary>
        /// <param name="disposing">This is <see langword="true"/> when <see cref="Dispose"/> was called, and <see langword="false"/>
        /// when the instance is being disposed on the finalizer thread.</param>
        protected virtual void OnDispose(bool disposing)
        {
        }

        /// <summary>
        /// Gets the <see cref="Status"/> of the client.
        /// </summary>
        /// <param name="message">A detailed explanation of the current status.</param>
        /// <returns>The status of this client.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public virtual Status GetStatus(out string message)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));

            message = string.Empty;
            return Status.None;
        }

        /// <summary>
        /// Sends an OSC Bundle containing all recent OSC Messages and OSC Bundles.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="AutoBundle"/> is <see langword="true"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <seealso cref="AutoBundle"/>
        public void SendAutoBundle()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));

            if (m_AutoBundle && !IsWriting)
            {
                EndBundleInternal();
                BeginBundle();
            }
        }

        /// <summary>
        /// Sends a pre-formatted OSC Message or OSC Bundle.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Instead of incrementally building messages using <see cref="BeginMessage(OscAddress,string)"/>, <see cref="EndMessage"/>, etc., you
        /// can use this to send a given message. This may be useful when implementing non-standard OSC messages. No validation is performed on
        /// the message, it is assumed to be a well-formatted OSC Bundle Element.
        /// </para>
        /// <para>
        /// If this is called after <see cref="BeginBundle()"/>, the provided message will be nested into current bundle.
        /// </para>
        /// </remarks>
        /// <param name="data">The bytes of the message to send.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="ArgumentException">Thrown if the length of <paramref name="data"/> is not a multiple of four.</exception>
        public void Send(ReadOnlySpan<byte> data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (data.Length % 4 != 0)
                throw new ArgumentException("Length must be a multiple of four.", nameof(data));

            var writer = BeginElement();

            fixed (byte* dataPtr = data)
            {
                writer.WriteCustom(dataPtr, data.Length);
            }

            EndElement(writer);
        }

        /// <summary>
        /// Sends a pre-formatted OSC Message or OSC Bundle.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Instead of incrementally building messages using <see cref="BeginMessage(OscAddress,string)"/>,<see cref="EndMessage"/>, etc., you
        /// can use this to send a given message. This may be useful when implementing non-standard OSC messages. No validation is performed on
        /// the message, it is assumed to be a well-formatted OSC Bundle Element.
        /// </para>
        /// <para>
        /// If this is called after <see cref="BeginBundle()"/>, the provided message will be nested into current bundle.
        /// </para>
        /// </remarks>
        /// <param name="data">The bytes of the message to send.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="ArgumentException">Thrown if the length of <paramref name="data"/> is not a multiple of four.</exception>
        public void Send(NativeSlice<byte> data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (data.Length % 4 != 0)
                throw new ArgumentException("Length must be a multiple of four.", nameof(data));

            var writer = BeginElement();

            writer.WriteCustom(data.GetUnsafeReadOnlyPtr(), data.Length);

            EndElement(writer);
        }

        /// <summary>
        /// Starts a new bundle to send.
        /// </summary>
        /// <remarks>
        /// Each call to this must be paired with a following call to <see cref="EndBundle"/> in order
        /// to complete the bundle. Calling <see cref="BeginMessage(OscAddress,string)"/>
        /// or <see cref="BeginBundle()"/> before then will cause the new element to be nested within the bundle.
        /// </remarks>
        /// <returns>A <see cref="BundleScope"/> which represents the new bundle.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <seealso cref="BeginBundle(TimeTag)"/>
        /// <seealso cref="EndBundle()"/>
        public BundleScope BeginBundle()
        {
            return BeginBundle(TimeTag.Now);
        }

        /// <summary>
        /// Starts a new bundle to send with the given time tag.
        /// </summary>
        /// <remarks>
        /// Each call to this must be paired with a following call to <see cref="EndBundle"/> in order
        /// to complete the bundle. Calling <see cref="BeginMessage(OscAddress,string)"/>
        /// or <see cref="BeginBundle()"/> before then will cause the new element to be nested within the bundle.
        /// </remarks>
        /// <param name="timeTag">The time tag to use for the bundle.</param>
        /// <returns>A <see cref="BundleScope"/> which represents the new bundle.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <seealso cref="BeginBundle()"/>
        /// <seealso cref="EndBundle()"/>
        public BundleScope BeginBundle(TimeTag timeTag)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));

            var writer = BeginElement();
            writer.WriteBundlePrefix();
            writer.WriteTimeTag(timeTag);

            // push the new bundle to the stack
            m_ElementStack[++m_ElementDepth] = new ElementState
            {
                Writer = writer,
                Type = Type.Bundle,
            };

            return new BundleScope(this);
        }

        /// <summary>
        /// Ends the current bundle.
        /// </summary>>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a bundle.</exception>
        /// <seealso cref="BeginBundle()"/>
        /// <seealso cref="BeginBundle(TimeTag)"/>
        public void EndBundle()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < (m_AutoBundle ? 1 : 0) || m_ElementStack[m_ElementDepth].Type != Type.Bundle)
                throw new InvalidOperationException("A bundle is not currently started.");

            EndBundleInternal();
        }

        /// <summary>
        /// Starts a new message to send.
        /// </summary>
        /// <remarks>
        /// Each call to this must be paired with a following call to <see cref="EndMessage"/> in order
        /// to complete the message.
        /// </remarks>
        /// <param name="address">The address pattern to use for the message.</param>
        /// <param name="tags">The tag string to use for the message. A tag string should be prefixed with a comma
        /// and include characters that correspond to a <see cref="TypeTag"/>.</param>
        /// <returns>A <see cref="MessageScope"/> which represents the new message.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="address"/> is not a valid address pattern.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tags"/> is null.</exception>
        /// <seealso cref="BeginMessage(OscAddress, TypeTag[])"/>
        /// <seealso cref="EndMessage()"/>
        public MessageScope BeginMessage(OscAddress address, string tags)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (address.Type == AddressType.Invalid)
                throw new ArgumentException("Address cannot be invalid.", nameof(address));
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            var writer = BeginElement();
            writer.WriteString(address);
            writer.WriteString(tags);

            // push the new message to the stack
            m_ElementStack[++m_ElementDepth] = new ElementState
            {
                Writer = writer,
                Type = Type.Message,
            };

            return new MessageScope(this);
        }

        /// <summary>
        /// Starts a new message to send.
        /// </summary>
        /// <remarks>
        /// Each call to this must be paired with a following call to <see cref="EndMessage"/> in order
        /// to complete the message.
        /// </remarks>
        /// <param name="address">The address pattern to use for the message.</param>
        /// <param name="tags">The tags to use for the message.</param>
        /// <returns>A <see cref="MessageScope"/> which represents the new message.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="address"/> is not a valid address pattern.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tags"/> is null.</exception>
        /// <seealso cref="BeginMessage(OscAddress, string)"/>
        /// <seealso cref="EndMessage()"/>
        public MessageScope BeginMessage(OscAddress address, params TypeTag[] tags)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (address.Type == AddressType.Invalid)
                throw new ArgumentException("Address cannot be invalid.", nameof(address));
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            using var tagString = OscUtils.CreateTagString(tags, Allocator.Temp);

            var writer = BeginElement();
            writer.WriteString(address);
            writer.WriteString(tagString);

            // push the new message to the stack
            m_ElementStack[++m_ElementDepth] = new ElementState
            {
                Writer = writer,
                Type = Type.Message,
            };

            return new MessageScope(this);
        }

        /// <summary>
        /// Ends the current message.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        /// <seealso cref="BeginMessage(OscAddress, string)"/>
        /// <seealso cref="BeginMessage(OscAddress, TypeTag[])"/>
        public void EndMessage()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth--].Writer;

            EndElement(writer);
        }

        /// <summary>
        /// Write a 32-bit integer element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteInt32(int data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteInt32(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write a 64-bit integer element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteInt64(long data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteInt64(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write a 32-bit float element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteFloat32(float data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteFloat32(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write a 64-bit float element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteFloat64(double data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteFloat64(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write a single ASCII character element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteChar(char data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteChar(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write an ASCII string element into the message buffer.
        /// </summary>
        /// <remarks>
        /// The string must only use ASCII characters. It is up to the caller to check for correctness.
        /// </remarks>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="data"/> is null.</exception>
        public void WriteString(string data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteString(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write an ASCII string element into the message buffer.
        /// </summary>
        /// <remarks>
        /// The string must only use ASCII characters. It is up to the caller to check for correctness.
        /// </remarks>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteString(NativeSlice<byte> data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteString(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write an blob element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteBlob(ReadOnlySpan<byte> data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteBlob(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write an blob element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteBlob(NativeSlice<byte> data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteBlob(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write a 32-bit RGBA color element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteColor(Color32 data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteColor(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write a MIDI message element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteMidi(MidiMessage data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteMidi(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        /// <summary>
        /// Write a time tag element into the current message.
        /// </summary>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the current element being written is not a message.</exception>
        public void WriteTimeTag(TimeTag data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscClient));
            if (m_ElementDepth < 0 || m_ElementStack[m_ElementDepth].Type != Type.Message)
                throw new InvalidOperationException("A message is not currently started.");

            var writer = m_ElementStack[m_ElementDepth].Writer;
            writer.WriteTimeTag(data);
            m_ElementStack[m_ElementDepth].Writer = writer;
        }

        OscWriter BeginElement()
        {
            if (m_ElementDepth >= 0)
            {
                var parentElement = m_ElementStack[m_ElementDepth];
                var writer = parentElement.Writer.FromCurrent();

                switch (parentElement.Type)
                {
                    case Type.Message:
                    {
                        throw new InvalidOperationException("Messages cannot contain nested bundle elements.");
                    }
                    case Type.Bundle:
                    {
                        // leave space for the element size
                        writer.SetOffset(sizeof(int));
                        break;
                    }
                }

                return writer;
            }

            return new OscWriter(m_Packet.BufferPtr, m_Packet.Buffer.Length);
        }

        void EndBundleInternal()
        {
            var writer = m_ElementStack[m_ElementDepth--].Writer;

            EndElement(writer, 16);
        }

        void EndElement(OscWriter writer, int minSize = 0)
        {
            if (m_ElementDepth >= 0)
            {
                var parentElement = m_ElementStack[m_ElementDepth];

                // the parent element data must continue from the end of the child element data
                var parentWriter = parentElement.Writer;
                parentWriter.SetOffset(parentWriter.Offset + writer.Offset);
                m_ElementStack[m_ElementDepth].Writer = parentWriter;

                if (parentElement.Type == Type.Bundle)
                {
                    // prefix the completed bundle element by its size
                    var length = writer.Offset - sizeof(int);
                    writer.SetOffset(0);
                    writer.WriteInt32(length);

                    // if the parent element is an auto-bundle, send the bundle if the size threshold has been reached
                    if (m_ElementDepth == 0 && m_AutoBundle && parentWriter.Offset >= m_AutoBundleThreshold)
                    {
                        EndBundleInternal();
                        BeginBundle();
                    }
                }
            }
            else if (IsReady && writer.Offset > minSize)
            {
                // We should not send messages that are too small to contain useful data, such
                // as empty bundles.
                OnSendPacket(m_Packet.Buffer, writer.Offset);
                MonitorPacket(m_Packet, writer.Offset);
            }
        }

        /// <summary>
        /// Sends an OSC Packet from this client.
        /// </summary>
        /// <remarks>
        /// This is not called when <see cref="IsReady"/> is <see langword="false"/>.
        /// </remarks>
        /// <param name="buffer">The buffer containing the packet. The packet must be copied if it
        /// is needed after this method returns, as the caller will modify the buffer contents.</param>
        /// <param name="size">The length of the packet in bytes.</param>
        protected abstract void OnSendPacket(byte[] buffer, int size);

        /// <summary>
        /// A struct which represents a bundle being written.
        /// </summary>
        public readonly struct BundleScope : IDisposable
        {
            readonly OscClient m_Client;

            internal BundleScope(OscClient client)
            {
                m_Client = client;
            }

            /// <summary>
            /// Disposes this instance.
            /// </summary>
            void IDisposable.Dispose()
            {
                m_Client.EndBundle();
            }
        }

        /// <summary>
        /// A struct which represents a message being written.
        /// </summary>
        public readonly struct MessageScope : IDisposable
        {
            readonly OscClient m_Client;

            internal MessageScope(OscClient client)
            {
                m_Client = client;
            }

            /// <summary>
            /// Disposes this instance.
            /// </summary>
            void IDisposable.Dispose()
            {
                m_Client.EndMessage();
            }
        }

        void MonitorPacket(OscPacket packet, int size)
        {
            if (!IsMonitorCallbackRegistered)
                return;

            packet.Parse(size);

            var root = packet.RootElement;

            if (!root.IsValid)
                return;

            switch (root)
            {
                case OscMessage message:
                {
                    HandleMessage(message);
                    break;
                }
                case OscBundle bundle:
                {
                    HandleBundle(bundle);
                    break;
                }
            }
        }

        void HandleBundle(OscBundle bundle)
        {
            for (var i = 0; i < bundle.MessageCount; i++)
            {
                var message = bundle.GetMessage(i);

                if (message.IsValid)
                {
                    HandleMessage(message);
                }
            }

            for (var i = 0; i < bundle.BundleCount; i++)
            {
                var nestedBundle = bundle.GetBundle(i);

                if (nestedBundle.IsValid)
                {
                    HandleBundle(nestedBundle);
                }
            }
        }

        void HandleMessage(OscMessage message)
        {
            var destination = Destination;

            if (IsMonitorCallbackRegistered)
            {
                MessageSent.Invoke(this, message, destination);
            }
        }
    }
}
