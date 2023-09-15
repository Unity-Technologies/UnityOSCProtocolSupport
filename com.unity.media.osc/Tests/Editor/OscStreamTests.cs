using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Media.Osc.Tests.Editor
{
    public class OscStreamTests
    {
        [Test]
        public void TestLengthPrefixStream()
        {
            // The data is not important for this test, we just need
            // to make sure we get the same thing out that we put in.
            var srcBuffer = new byte[]
            {
                0x2f, 0x66, 0x6f, 0x6f,
                0x00, 0xC0, 0x00, 0x00,
                0x2c, 0x46, 0x49, 0x4E,
                0x53, 0x54, 0x62, 0x63,
                0x64, 0x66, 0x68, 0x69,
                0x6D, 0x72, 0x73, 0x74,
                0x00, 0xDD, 0xDB, 0xDB,
                0x61, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x04,
                0x10, 0x20, 0x30, 0x40,
                0x00, 0x00, 0xDC, 0x61,
                0x41, 0x3F, 0x0A, 0x3A,
                0x1F, 0x0D, 0x84, 0x4D,
                0x49, 0xf8, 0x51, 0xd1,
                0xC0, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0xDD,
                0xff, 0xff, 0xff, 0xff,
                0x22, 0x33, 0x44, 0x55,
                0x10, 0x20, 0x30, 0x40,
                0x66, 0xDB, 0x6f, 0x6f,
                0x00, 0x00, 0xDC, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01,
            };

            var readPacketCount = 0;

            using var stream = new MemoryStream();
            using var streamWriter = new LengthPrefixStreamWriter(stream);
            using var streamReader = new LengthPrefixStreamReader(stream, (packet, length) =>
            {
                readPacketCount++;

                Assert.AreEqual(srcBuffer.Length, length);

                for (var i = 0; i < length; i++)
                {
                    Assert.AreEqual(srcBuffer[i], packet.Buffer[i]);
                }
            });

            readPacketCount = 0;
            streamReader.ReadAllPackets();

            Assert.AreEqual(0, readPacketCount);

            streamWriter.WriteToStream(srcBuffer, srcBuffer.Length);

            Assert.AreEqual(srcBuffer.Length + sizeof(int), stream.Length);

            stream.Position = 0;
            readPacketCount = 0;
            streamReader.ReadAllPackets();

            Assert.AreEqual(1, readPacketCount);

            streamWriter.WriteToStream(srcBuffer, srcBuffer.Length);

            stream.Position = 0;
            readPacketCount = 0;
            streamReader.ReadAllPackets();

            Assert.AreEqual(2, readPacketCount);
        }

        [Test]
        public void TestLengthPrefixReadInvalidSizeException()
        {
            using var stream = new MemoryStream(new byte[]
            {
                0x00, 0x00, 0x00, 0x10,
            });

            using var streamReader = new LengthPrefixStreamReader(
                stream,
                (packet, length) =>
                {
                },
                10
            );

            Assert.Throws<InvalidPacketSizeException>(() => streamReader.ReadAllPackets());
        }

        [Test]
        public void TesSlipStream()
        {
            // There are some values in here that need to be escaped, which is important
            // to test for. We expect the same data out that we put in.
            var srcBuffer = new byte[]
            {
                0x2f, 0x66, 0x6f, 0x6f,
                0x00, 0xC0, 0x00, 0x00,
                0x2c, 0x46, 0x49, 0x4E,
                0x53, 0x54, 0x62, 0x63,
                0x64, 0x66, 0x68, 0x69,
                0x6D, 0x72, 0x73, 0x74,
                0x00, 0xDD, 0xDB, 0xDB,
                0x61, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x04,
                0x10, 0x20, 0x30, 0x40,
                0x00, 0x00, 0xDC, 0x61,
                0x41, 0x3F, 0x0A, 0x3A,
                0x1F, 0x0D, 0x84, 0x4D,
                0x49, 0xf8, 0x51, 0xd1,
                0xC0, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0xDD,
                0xff, 0xff, 0xff, 0xff,
                0x22, 0x33, 0x44, 0x55,
                0x10, 0x20, 0x30, 0x40,
                0x66, 0xDB, 0x6f, 0x6f,
                0x00, 0x00, 0xDC, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01,
            };
            var encodedBuffer = new byte[]
            {
                0xC0,
                0x2f, 0x66, 0x6f, 0x6f,
                0x00, 0xDB, 0xDC, 0x00, 0x00,
                0x2c, 0x46, 0x49, 0x4E,
                0x53, 0x54, 0x62, 0x63,
                0x64, 0x66, 0x68, 0x69,
                0x6D, 0x72, 0x73, 0x74,
                0x00, 0xDD, 0xDB, 0xDD, 0xDB, 0xDD,
                0x61, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x04,
                0x10, 0x20, 0x30, 0x40,
                0x00, 0x00, 0xDC, 0x61,
                0x41, 0x3F, 0x0A, 0x3A,
                0x1F, 0x0D, 0x84, 0x4D,
                0x49, 0xf8, 0x51, 0xd1,
                0xDB, 0xDC, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0xDD,
                0xff, 0xff, 0xff, 0xff,
                0x22, 0x33, 0x44, 0x55,
                0x10, 0x20, 0x30, 0x40,
                0x66, 0xDB, 0xDD, 0x6f, 0x6f,
                0x00, 0x00, 0xDC, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01,
                0xC0,
            };

            var readPacketCount = 0;

            using var stream = new MemoryStream();
            using var streamWriter = new SlipStreamWriter(stream);
            using var streamReader = new SlipStreamReader(stream, (packet, length) =>
            {
                readPacketCount++;

                Assert.AreEqual(srcBuffer.Length, length);

                for (var i = 0; i < length; i++)
                {
                    Assert.AreEqual(srcBuffer[i], packet.Buffer[i]);
                }
            });

            readPacketCount = 0;
            streamReader.ReadAllPackets();

            Assert.AreEqual(0, readPacketCount);

            streamWriter.WriteToStream(srcBuffer, srcBuffer.Length);
            stream.Position = 0;

            Assert.AreEqual(encodedBuffer.Length, stream.Length, "There should be five escaped bytes, each adding an extra byte to the message length, and one END byte added to the start and end.");

            for (var i = 0; i < encodedBuffer.Length; i++)
            {
                Assert.AreEqual(encodedBuffer[i], stream.ReadByte());
            }

            stream.Position = 0;
            readPacketCount = 0;
            streamReader.ReadAllPackets();

            Assert.AreEqual(1, readPacketCount);

            streamWriter.WriteToStream(srcBuffer, srcBuffer.Length);

            stream.Position = 0;
            readPacketCount = 0;
            streamReader.ReadAllPackets();

            Assert.AreEqual(2, readPacketCount);
        }
    }
}
