using System;
using System.IO;
using System.Net.Sockets;

namespace Unity.Media.Osc
{
    class SocketStream : NetworkStream
    {
        public new Socket Socket { get; }

        public SocketStream(Socket socket, FileAccess access) : base(socket, access, true)
        {
            Socket = socket;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return DataAvailable ? base.Read(buffer, offset, count) : 0;
        }
    }
}
