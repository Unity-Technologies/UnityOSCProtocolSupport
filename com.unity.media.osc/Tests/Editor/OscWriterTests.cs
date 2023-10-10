using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Media.Osc.Tests.Editor
{
    public unsafe class OscWriterTests
    {
        [Test]
        public void TestConstructor()
        {
            Assert.Catch<ArgumentOutOfRangeException>(() => new OscWriter((byte*)0, -1));

            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                Assert.AreEqual(0, writer.Offset);
            }
        }

        [Test]
        public void TestSetOffset()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                Assert.Catch<ArgumentOutOfRangeException>(() => writer.SetOffset(-1));

                writer.SetOffset(0);
                Assert.AreEqual(0, writer.Offset);

                writer.SetOffset(8);
                Assert.AreEqual(8, writer.Offset);

                writer.SetOffset(buffer.Length - 1);
                Assert.AreEqual(buffer.Length - 1, writer.Offset);

                Assert.Catch<ArgumentOutOfRangeException>(() => writer.SetOffset(buffer.Length));
                Assert.Catch<ArgumentOutOfRangeException>(() => writer.SetOffset(buffer.Length + 1));
            }
        }

        [Test]
        public void TestClear()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.SetOffset(8);
                Assert.AreEqual(8, writer.Offset);

                writer.Clear();
                Assert.AreEqual(0, writer.Offset);
            }
        }

        [Test]
        public void TestFromCurrent()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);
                writer.SetOffset(8);

                var newWriter = writer.FromCurrent();

                writer.SetOffset(7);
                Assert.AreEqual(7, writer.Offset);

                Assert.Catch<ArgumentOutOfRangeException>(() => newWriter.SetOffset(8));
            }
        }

        [Test]
        public void TestWriteBundlePrefix()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.WriteBundlePrefix();
                Assert.AreEqual(8, writer.Offset);

                writer.SetOffset(9);
                Assert.Catch<InvalidOperationException>(() => writer.WriteBundlePrefix());
            }

            var expectedBuffer = new[]
            {
                (byte)'#',
                (byte)'b',
                (byte)'u',
                (byte)'n',
                (byte)'d',
                (byte)'l',
                (byte)'e',
                (byte)'\0',
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteInt32()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.WriteInt32(0x5E78407E);
                Assert.AreEqual(4, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteInt32(0x5E78407E));
            }

            var expectedBuffer = new[]
            {
                (byte)0x5E,
                (byte)0x78,
                (byte)0x40,
                (byte)0x7E,
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteInt64()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.WriteInt64(0x5E78407E3A24D2C4L);
                Assert.AreEqual(8, writer.Offset);

                writer.SetOffset(9);
                Assert.Catch<InvalidOperationException>(() => writer.WriteInt64(0x5E78407E3A24D2C4L));
            }

            var expectedBuffer = new[]
            {
                (byte)0x5E,
                (byte)0x78,
                (byte)0x40,
                (byte)0x7E,
                (byte)0x3A,
                (byte)0x24,
                (byte)0xD2,
                (byte)0xC4,
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteFloat32()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.WriteFloat32(2793785344.0f);
                Assert.AreEqual(4, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteFloat32(2793785344.0f));
            }

            var expectedBuffer = new[]
            {
                (byte)0x4f,
                (byte)0x26,
                (byte)0x85,
                (byte)0xc8,
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteFloat64()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.WriteFloat64(2793785344.29384719);
                Assert.AreEqual(8, writer.Offset);

                writer.SetOffset(9);
                Assert.Catch<InvalidOperationException>(() => writer.WriteFloat64(2793785344.29384719));
            }

            var expectedBuffer = new[]
            {
                (byte)0x41,
                (byte)0xE4,
                (byte)0xD0,
                (byte)0xB9,
                (byte)0x00,
                (byte)0x09,
                (byte)0x67,
                (byte)0x32,
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteCharacter()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.WriteChar('A');
                Assert.AreEqual(4, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteChar('A'));
            }

            var expectedBuffer = new[]
            {
                (byte)0,
                (byte)0,
                (byte)0,
                (byte)'A',
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteStringNull()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);
                Assert.Catch<ArgumentNullException>(() => writer.WriteString(null));
            }
        }

        static IEnumerable TestWriteStringData
        {
            get
            {
                yield return new TestCaseData("TestString", "TestString\0\0");
                yield return new TestCaseData("01234567", "01234567\0\0\0\0");
                yield return new TestCaseData("012345678", "012345678\0\0\0");
                yield return new TestCaseData("0123456789", "0123456789\0\0");
                yield return new TestCaseData("01234567890", "01234567890\0");
                yield return new TestCaseData("012345678901", "012345678901\0\0\0\0");
            }
        }

        [Test, TestCaseSource(nameof(TestWriteStringData))]
        public void TestWriteString(string str, string expectedStr)
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                writer.WriteString(str);
                Assert.AreEqual(expectedStr.Length, writer.Offset);

                writer.SetOffset(15);
                Assert.Catch<InvalidOperationException>(() => writer.WriteString(str));
            }

            for (var i = 0; i < expectedStr.Length; i++)
            {
                Assert.AreEqual((byte)expectedStr[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteStringSlice()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                using var str = new NativeArray<byte>(new[]
                {
                    (byte)'T',
                    (byte)'e',
                    (byte)'s',
                    (byte)'t',
                    (byte)'S',
                    (byte)'t',
                    (byte)'r',
                    (byte)'i',
                    (byte)'n',
                    (byte)'g',
                    (byte)'E',
                    (byte)'x',
                    (byte)'t',
                    (byte)'r',
                    (byte)'a',
                }, Allocator.Temp);

                var slice = str.Slice(0, 10);

                writer.WriteString(slice);
                Assert.AreEqual(12, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteString(slice));
            }

            var expectedBuffer = new[]
            {
                (byte)'T',
                (byte)'e',
                (byte)'s',
                (byte)'t',
                (byte)'S',
                (byte)'t',
                (byte)'r',
                (byte)'i',
                (byte)'n',
                (byte)'g',
                (byte)'\0',
                (byte)'\0',
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteStringAddress()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                using var address = new OscAddress("root/method");
                writer.WriteString(address);
                Assert.AreEqual(12, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteString(address));
            }

            var expectedBuffer = new[]
            {
                (byte)'r',
                (byte)'o',
                (byte)'o',
                (byte)'t',
                (byte)'/',
                (byte)'m',
                (byte)'e',
                (byte)'t',
                (byte)'h',
                (byte)'o',
                (byte)'d',
                (byte)'\0',
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteBlob()
        {
            var buffer = new byte[32];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                var blob = new[]
                {
                    (byte)'a',
                    (byte)'a',
                    (byte)'T',
                    (byte)'e',
                    (byte)'s',
                    (byte)'t',
                    (byte)'B',
                    (byte)'l',
                    (byte)'o',
                    (byte)'b',
                    (byte)'0',
                    (byte)'1',
                };

                var slice = blob.AsSpan(2, 10);

                writer.WriteBlob(slice);
                Assert.AreEqual(16, writer.Offset);

                writer.SetOffset(30);
                Assert.Catch<InvalidOperationException>(() => writer.WriteBlob(blob.AsSpan(2, 10)));
            }

            var expectedBuffer = new[]
            {
                (byte)0,
                (byte)0,
                (byte)0,
                (byte)10,
                (byte)'T',
                (byte)'e',
                (byte)'s',
                (byte)'t',
                (byte)'B',
                (byte)'l',
                (byte)'o',
                (byte)'b',
                (byte)'0',
                (byte)'1',
                (byte)'\0',
                (byte)'\0',
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteBlobSlice()
        {
            var buffer = new byte[32];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                using var array = new NativeArray<byte>(new[]
                {
                    (byte)'a',
                    (byte)'a',
                    (byte)'T',
                    (byte)'e',
                    (byte)'s',
                    (byte)'t',
                    (byte)'B',
                    (byte)'l',
                    (byte)'o',
                    (byte)'b',
                }, Allocator.Temp);

                var slice = array.Slice(2, 8);

                writer.WriteBlob(slice);
                Assert.AreEqual(12, writer.Offset);

                writer.SetOffset(30);
                Assert.Catch<InvalidOperationException>(() => writer.WriteBlob(slice));
            }

            var expectedBuffer = new[]
            {
                (byte)0,
                (byte)0,
                (byte)0,
                (byte)8,
                (byte)'T',
                (byte)'e',
                (byte)'s',
                (byte)'t',
                (byte)'B',
                (byte)'l',
                (byte)'o',
                (byte)'b',
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteColor32()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                var color = new Color32(20, 80, 120, 180);
                writer.WriteColor(color);
                Assert.AreEqual(4, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteColor(color));
            }

            var expectedBuffer = new[]
            {
                (byte)20,
                (byte)80,
                (byte)120,
                (byte)180,
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteMidi()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                var midi = new MidiMessage(20, 80, 120, 180);
                writer.WriteMidi(midi);
                Assert.AreEqual(4, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteMidi(midi));
            }

            var expectedBuffer = new[]
            {
                (byte)20,
                (byte)80,
                (byte)120,
                (byte)180,
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }

        [Test]
        public void TestWriteTimeTag()
        {
            var buffer = new byte[16];

            fixed (byte* bufferPtr = buffer)
            {
                var writer = new OscWriter(bufferPtr, buffer.Length);

                var timeTag = new TimeTag(0x7D51817Fu, 0x30FAAFDAu);
                writer.WriteTimeTag(timeTag);
                Assert.AreEqual(8, writer.Offset);

                writer.SetOffset(13);
                Assert.Catch<InvalidOperationException>(() => writer.WriteTimeTag(timeTag));
            }

            var expectedBuffer = new[]
            {
                (byte)0x7D,
                (byte)0x51,
                (byte)0x81,
                (byte)0x7F,
                (byte)0x30,
                (byte)0xFA,
                (byte)0xAF,
                (byte)0xDA,
            };

            for (var i = 0; i < expectedBuffer.Length; i++)
            {
                Assert.AreEqual(expectedBuffer[i], buffer[i]);
            }
        }
    }
}
