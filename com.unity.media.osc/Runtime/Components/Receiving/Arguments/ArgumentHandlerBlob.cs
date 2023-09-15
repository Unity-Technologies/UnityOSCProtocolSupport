using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Blob")]
    sealed class ArgumentHandlerBlob : ArgumentHandler<byte[]>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out byte[] value)
        {
            if (message.TryAccessBlob(argumentIndex, out _, out var length))
            {
                value = new byte[length];
                message.ReadBlob(argumentIndex, ref value);
                return true;
            }

            value = null;
            return false;
        }
    }
}
