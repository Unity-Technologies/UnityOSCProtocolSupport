using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Media.Osc.Tests.Editor
{
    public class OscParsingTests
    {
        byte[] m_Buffer;
        OscPacket m_Packet;

        [OneTimeSetUp]
        public void TestSetup()
        {
            m_Buffer = new byte[1024];
            m_Packet = new OscPacket(m_Buffer);
        }

        [OneTimeTearDown]
        public void TestTearDown()
        {
            m_Packet.Dispose();
        }

        [Test]
        public void TestPacketConstructor()
        {
            Assert.Catch<ArgumentNullException>(() => new OscPacket(null));

            var packet = new OscPacket(new byte[0]);

            Assert.IsNull(packet.RootElement);
        }

        [Test]
        public void TestPacketParseInvalidSize()
        {
            Assert.Catch<ArgumentOutOfRangeException>(() => m_Packet.Parse(-1, 0));
        }

        [Test]
        public void TestPacketParseInvalidOffset()
        {
            Assert.Catch<ArgumentOutOfRangeException>(() => m_Packet.Parse(10, -1));
        }

        [Test]
        public void TestPacketParseInvalidBufferToSmall()
        {
            Assert.Catch<ArgumentException>(() => m_Packet.Parse(1000, 1000));
        }

        static IEnumerable TestParseMessageData
        {
            get
            {
                // from the official examples: https://opensoundcontrol.stanford.edu/spec-1_0-examples.html
                yield return new TestCaseData(
                    new byte[]
                    {
                        0x2f, 0x6f, 0x73, 0x63, // address
                        0x69, 0x6c, 0x6c, 0x61,
                        0x74, 0x6f, 0x72, 0x2f,
                        0x34, 0x2f, 0x66, 0x72,
                        0x65, 0x71, 0x75, 0x65,
                        0x6e, 0x63, 0x79, 0x00,
                        0x2c, 0x66, 0x00, 0x00, // tags
                        0x43, 0xdc, 0x00, 0x00, // arguments
                    },
                    "/oscillator/4/frequency",
                    new TypeTag[] { TypeTag.Float32, },
                    new Action<OscMessage, int>((message, index) =>
                    {
                        Assert.AreEqual(440.0f, message.ReadFloat32(index));
                    })
                );
                yield return new TestCaseData(
                    new byte[]
                    {
                        0x2f, 0x66, 0x6f, 0x6f, // address
                        0x00, 0x00, 0x00, 0x00,
                        0x2c, 0x69, 0x69, 0x73, // tags
                        0x66, 0x66, 0x00, 0x00,
                        0x00, 0x00, 0x03, 0xe8, // arguments
                        0xff, 0xff, 0xff, 0xff,
                        0x68, 0x65, 0x6c, 0x6c,
                        0x6f, 0x00, 0x00, 0x00,
                        0x3f, 0x9d, 0xf3, 0xb6,
                        0x40, 0xb5, 0xb2, 0x2d,
                    },
                    "/foo",
                    new TypeTag[] { TypeTag.Int32, TypeTag.Int32, TypeTag.String, TypeTag.Float32, TypeTag.Float32 },
                    new Action<OscMessage, int>((message, index) =>
                    {
                        switch (index)
                        {
                            case 0:
                                Assert.AreEqual(1000, message.ReadInt32(index));
                                break;
                            case 1:
                                Assert.AreEqual(-1, message.ReadInt32(index));
                                break;
                            case 2:
                                Assert.AreEqual("hello", message.ReadString(index));
                                break;
                            case 3:
                                Assert.AreEqual(1.234f, message.ReadFloat32(index));
                                break;
                            case 4:
                                Assert.AreEqual(5.678f, message.ReadFloat32(index));
                                break;
                        }
                    })
                );
                yield return new TestCaseData(
                    new byte[]
                    {
                        0x2f, 0x66, 0x6f, 0x6f, // address
                        0x00, 0x00, 0x00, 0x00,
                        0x2c, 0x69, 0x69, 0x66, // tags
                        0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x03, 0xe8, // arguments
                        0xff, 0xff, 0xff, 0xff,
                        0x3f, 0x9d, 0xf3, 0xb6,
                    },
                    "/foo",
                    new TypeTag[] { TypeTag.Int32, TypeTag.Int32, TypeTag.Float32 },
                    new Action<OscMessage, int>((message, index) =>
                    {
                        switch (index)
                        {
                            case 0:
                                Assert.AreEqual(1000, message.ReadInt32(index));
                                break;
                            case 1:
                                Assert.AreEqual(-1, message.ReadInt32(index));
                                break;
                            case 2:
                                Assert.AreEqual(1.234f, message.ReadFloat32(index));
                                break;
                        }
                    })
                );
                yield return new TestCaseData(
                    new byte[]
                    {
                        0x2f, 0x66, 0x6f, 0x6f, // address
                        0x00, 0x00, 0x00, 0x00,
                        0x2c, 0x46, 0x49, 0x4E, // tags
                        0x53, 0x54, 0x62, 0x63,
                        0x64, 0x66, 0x68, 0x69,
                        0x6D, 0x72, 0x73, 0x74,
                        0x00, 0x00, 0x00, 0x00,
                        0x61, 0x00, 0x00, 0x00, // arguments
                        0x00, 0x00, 0x00, 0x04,
                        0x10, 0x20, 0x30, 0x40,
                        0x00, 0x00, 0x00, 0x61,
                        0x41, 0x3F, 0x0A, 0x3A,
                        0x1F, 0x0D, 0x84, 0x4D,
                        0x49, 0xf8, 0x51, 0xd1,
                        0x00, 0x00, 0x00, 0x01,
                        0x00, 0x00, 0x00, 0x00,
                        0xff, 0xff, 0xff, 0xff,
                        0x22, 0x33, 0x44, 0x55,
                        0x10, 0x20, 0x30, 0x40,
                        0x66, 0x6f, 0x6f, 0x6f,
                        0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x01,
                    },
                    "/foo",
                    new TypeTag[]
                    {
                        TypeTag.False, TypeTag.Infinitum, TypeTag.Nil,
                        TypeTag.AltTypeString, TypeTag.True, TypeTag.Blob, TypeTag.AsciiChar32,
                        TypeTag.Float64, TypeTag.Float32, TypeTag.Int64, TypeTag.Int32,
                        TypeTag.MIDI, TypeTag.Color32, TypeTag.String, TypeTag.TimeTag,
                    },
                    new Action<OscMessage, int>((message, index) =>
                    {
                        switch (index)
                        {
                            case 3:
                                Assert.AreEqual("a", message.ReadString(index));
                                break;
                            case 5:
                                var blob = default(byte[]);
                                var length = message.ReadBlob(index, ref blob, 0);

                                Assert.AreEqual(4, length);
                                Assert.AreEqual(0x10, blob[0]);
                                Assert.AreEqual(0x20, blob[1]);
                                Assert.AreEqual(0x30, blob[2]);
                                Assert.AreEqual(0x40, blob[3]);
                                break;
                            case 6:
                                Assert.AreEqual('a', message.ReadAsciiChar(index));
                                break;
                            case 7:
                                Assert.AreEqual(2034234.1213, message.ReadFloat64(index));
                                break;
                            case 8:
                                Assert.AreEqual(2034234.1213f, message.ReadFloat32(index));
                                break;
                            case 9:
                                Assert.AreEqual((long)uint.MaxValue + 1, message.ReadInt64(index));
                                break;
                            case 10:
                                Assert.AreEqual(-1, message.ReadInt32(index));
                                break;
                            case 11:
                                var midi = message.ReadMidi(index);
                                Assert.AreEqual(0x22, midi.PortId);
                                Assert.AreEqual(0x33, midi.Status);
                                Assert.AreEqual(0x44, midi.Data1);
                                Assert.AreEqual(0x55, midi.Data2);
                                break;
                            case 12:
                                var color = message.ReadColor32(index);
                                Assert.AreEqual(0x10, color.r);
                                Assert.AreEqual(0x20, color.g);
                                Assert.AreEqual(0x30, color.b);
                                Assert.AreEqual(0x40, color.a);
                                break;
                            case 13:
                                Assert.AreEqual("fooo", message.ReadString(index));
                                break;
                            case 14:
                                Assert.AreEqual(TimeTag.Now, message.ReadTimeTag(index));
                                break;
                        }
                    })
                );
            }
        }

        [Test, TestCaseSource(nameof(TestParseMessageData))]
        public void TestParseMessage(byte[] packet, string address, TypeTag[] tags, Action<OscMessage, int> testTagIndex)
        {
            Array.Copy(packet, m_Buffer, packet.Length);

            m_Packet.Parse(packet.Length, 0);

            Assert.IsNotNull(m_Packet.RootElement);

            var message = m_Packet.RootElement as OscMessage;

            Assert.IsNotNull(message);
            Assert.IsTrue(message.IsValid);
            Assert.AreEqual(packet.Length, message.ElementSize);

            using var addr = new OscAddress(address, Allocator.Temp);

            Assert.AreEqual(addr, message.GetAddressPattern());
            Assert.AreEqual(tags.Length, message.ArgumentCount);

            Assert.Catch<ArgumentOutOfRangeException>(() => message.GetTag(-1));
            Assert.Catch<ArgumentOutOfRangeException>(() => message.GetTag(tags.Length + 1));

            for (var i = 0; i < tags.Length; i++)
            {
                Assert.AreEqual(tags[i], message.GetTag(i));
            }
            for (var i = 0; i < tags.Length; i++)
            {
                testTagIndex(message, i);
            }
        }

        [Test]
        public void TestBundleInvalid()
        {
            var packet = new byte[]
            {
                0x23, 0x62, 0x75, 0x6e, // bundle identifier
                0x64, 0x6c, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x00, // bundle time tag (invalid)
            };

            Array.Copy(packet, m_Buffer, packet.Length);

            m_Packet.Parse(packet.Length, 0);

            Assert.IsNotNull(m_Packet.RootElement);

            var bundle = m_Packet.RootElement as OscBundle;

            Assert.IsNotNull(bundle);
            Assert.IsFalse(bundle.IsValid);
        }

        [Test]
        public void TestBundleEmpty()
        {
            var packet = new byte[]
            {
                0x23, 0x62, 0x75, 0x6e, // bundle identifier
                0x64, 0x6c, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x00, // bundle time tag
                0x00, 0x00, 0x00, 0x01,
            };

            Array.Copy(packet, m_Buffer, packet.Length);

            m_Packet.Parse(packet.Length, 0);

            Assert.IsNotNull(m_Packet.RootElement);

            var bundle = m_Packet.RootElement as OscBundle;

            Assert.IsNotNull(bundle);
            Assert.IsTrue(bundle.IsValid);
            Assert.AreEqual(packet.Length, bundle.ElementSize);
            Assert.AreEqual(0, bundle.BundleCount);
            Assert.AreEqual(0, bundle.MessageCount);
            Assert.AreEqual(TimeTag.Now, bundle.GetTimeTag());
        }

        [Test]
        public void TestBundleTwoMessages()
        {
            var packet = new byte[]
            {
                0x23, 0x62, 0x75, 0x6e, // bundle identifier
                0x64, 0x6c, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x00, // bundle time tag
                0x00, 0x00, 0x00, 0x01,

                0x00, 0x00, 0x00, 0x10, // element size

                0x2f, 0x66, 0x6f, 0x6f, // address
                0x00, 0x00, 0x00, 0x00,
                0x2c, 0x66, 0x00, 0x00, // tags
                0x43, 0xdc, 0x00, 0x00, // arguments

                0x00, 0x00, 0x00, 0x10, // element size

                0x2f, 0x66, 0x6f, 0x6f, // address
                0x00, 0x00, 0x00, 0x00,
                0x2c, 0x69, 0x00, 0x00, // tags
                0xff, 0xff, 0xff, 0xff, // arguments
            };

            Array.Copy(packet, m_Buffer, packet.Length);

            m_Packet.Parse(packet.Length, 0);

            Assert.IsNotNull(m_Packet.RootElement);

            var bundle = m_Packet.RootElement as OscBundle;

            Assert.IsNotNull(bundle);
            Assert.IsTrue(bundle.IsValid);
            Assert.AreEqual(packet.Length, bundle.ElementSize);
            Assert.AreEqual(0, bundle.BundleCount);
            Assert.AreEqual(2, bundle.MessageCount);
            Assert.AreEqual(TimeTag.Now, bundle.GetTimeTag());

            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetBundle(-1));
            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetBundle(0));
            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetMessage(-1));
            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetMessage(3));

            Assert.AreEqual(440f, bundle.GetMessage(0).ReadFloat32(0));
            Assert.AreEqual(-1, bundle.GetMessage(1).ReadInt32(0));
        }

        [Test]
        public void TestBundleNestedBundles()
        {
            // two nested bundles with a message each
            var packet = new byte[]
            {
                0x23, 0x62, 0x75, 0x6e, // bundle identifier
                0x64, 0x6c, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x00, // bundle time tag
                0x00, 0x00, 0x00, 0x01,

                0x00, 0x00, 0x00, 0x24, // element size

                0x23, 0x62, 0x75, 0x6e, // bundle identifier
                0x64, 0x6c, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x00, // bundle time tag
                0x00, 0x00, 0x00, 0x01,

                0x00, 0x00, 0x00, 0x10, // element size

                0x2f, 0x66, 0x6f, 0x6f, // address
                0x00, 0x00, 0x00, 0x00,
                0x2c, 0x66, 0x00, 0x00, // tags
                0x43, 0xdc, 0x00, 0x00, // arguments

                0x00, 0x00, 0x00, 0x24, // element size

                0x23, 0x62, 0x75, 0x6e, // bundle identifier
                0x64, 0x6c, 0x65, 0x00,
                0x00, 0x00, 0x00, 0x00, // bundle time tag
                0x00, 0x00, 0x00, 0x01,

                0x00, 0x00, 0x00, 0x10, // element size

                0x2f, 0x66, 0x6f, 0x6f, // address
                0x00, 0x00, 0x00, 0x00,
                0x2c, 0x69, 0x00, 0x00, // tags
                0xff, 0xff, 0xff, 0xff, // arguments
            };

            Array.Copy(packet, m_Buffer, packet.Length);

            m_Packet.Parse(packet.Length, 0);

            Assert.IsNotNull(m_Packet.RootElement);

            var bundle = m_Packet.RootElement as OscBundle;

            Assert.IsNotNull(bundle);
            Assert.IsTrue(bundle.IsValid);
            Assert.AreEqual(packet.Length, bundle.ElementSize);
            Assert.AreEqual(2, bundle.BundleCount);
            Assert.AreEqual(0, bundle.MessageCount);
            Assert.AreEqual(TimeTag.Now, bundle.GetTimeTag());

            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetBundle(-1));
            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetBundle(3));
            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetMessage(-1));
            Assert.Catch<ArgumentOutOfRangeException>(() => bundle.GetMessage(0));

            Assert.AreEqual(440f, bundle.GetBundle(0).GetMessage(0).ReadFloat32(0));
            Assert.AreEqual(-1, bundle.GetBundle(1).GetMessage(0).ReadInt32(0));
        }
    }
}
