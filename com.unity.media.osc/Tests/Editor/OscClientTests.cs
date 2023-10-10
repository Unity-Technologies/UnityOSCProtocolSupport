using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Media.Osc.Tests.Editor
{
    public class OscClientTests
    {
        class TestClient : OscClient
        {
            public List<(byte[] packet, int size)> Sends { get; } = new List<(byte[] packet, int size)>();

            public TestClient(int bufferSize) : base(bufferSize)
            {
            }

            protected override void OnSendPacket(byte[] buffer, int size)
            {
                Sends.Add((buffer, size));
            }
        }

        [Test]
        public void TestConstructorNegative()
        {
            Assert.Catch<ArgumentOutOfRangeException>(() => new TestClient(-1));

            SuppressFinalizer();
        }

        [Test]
        public void TestConstructorZero()
        {
            Assert.Catch<ArgumentOutOfRangeException>(() => new TestClient(0));

            SuppressFinalizer();
        }

        void SuppressFinalizer()
        {
            // The object executes the finalizer even when ctor fails, but we log an error in the
            // finalizer causing the test to report an error. To fix this, we must suppress the assertion.
            Debug.unityLogger.logEnabled = false;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Debug.unityLogger.logEnabled = true;
        }

        [Test]
        public void TestConstructorValid()
        {
            using var client = new TestClient(20);

            Assert.IsFalse(client.IsDisposed);
            Assert.IsFalse(client.IsWriting);
            Assert.IsFalse(client.AutoBundle);
        }

        [Test]
        public void TestDispose()
        {
            using var client = new TestClient(20);

            client.Dispose();

            Assert.IsTrue(client.IsDisposed);

            Assert.DoesNotThrow(() => client.Dispose());

            Assert.IsTrue(client.IsDisposed);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestIsWritingMessage(bool autoBundle)
        {
            using var client = new TestClient(128)
            {
                AutoBundle = autoBundle
            };
            using var address = new OscAddress("/foo");

            Assert.IsFalse(client.IsWriting);

            client.BeginMessage(address, string.Empty);

            Assert.IsTrue(client.IsWriting);

            client.EndMessage();

            Assert.IsFalse(client.IsWriting);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestIsWritingBundle(bool autoBundle)
        {
            using var client = new TestClient(128)
            {
                AutoBundle = autoBundle
            };

            client.AutoBundle = false;

            Assert.IsFalse(client.IsWriting);

            client.BeginBundle();

            Assert.IsTrue(client.IsWriting);

            client.EndBundle();

            Assert.IsFalse(client.IsWriting);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestIsWritingNested(bool autoBundle)
        {
            using var client = new TestClient(128)
            {
                AutoBundle = autoBundle
            };
            using var address = new OscAddress("/foo");

            client.AutoBundle = false;

            Assert.IsFalse(client.IsWriting);

            client.BeginBundle();

            Assert.IsTrue(client.IsWriting);

            client.BeginBundle();

            Assert.IsTrue(client.IsWriting);

            client.BeginMessage(address, string.Empty);

            Assert.IsTrue(client.IsWriting);

            client.EndMessage();

            Assert.IsTrue(client.IsWriting);

            client.EndBundle();

            Assert.IsTrue(client.IsWriting);

            client.EndBundle();

            Assert.IsFalse(client.IsWriting);
        }

        [Test]
        public void TestIsWritingDisposed()
        {
            using var client = new TestClient(128);

            client.Dispose();

            Assert.Catch<ObjectDisposedException>(() => _ = client.IsWriting);
        }

        [Test]
        public void TestSetAutoBundleThrowsIfWriting()
        {
            using var client = new TestClient(128);

            Assert.DoesNotThrow(() => client.AutoBundle = true);

            client.BeginBundle();

            Assert.Catch<InvalidOperationException>(() => client.AutoBundle = false);
        }

        [Test]
        public void TestSendAutoBundleEmpty()
        {
            using var client = new TestClient(128)
            {
                AutoBundle = true,
                AutoBundleThreshold = int.MaxValue,
            };

            Assert.AreEqual(0, client.Sends.Count);

            client.SendAutoBundle();

            Assert.AreEqual(0, client.Sends.Count);
        }

        [Test]
        public void TestSendAutoBundle()
        {
            using var client = new TestClient(128)
            {
                AutoBundle = true,
                AutoBundleThreshold = int.MaxValue,
            };
            using var address = new OscAddress("/foo");

            client.BeginBundle();

            client.BeginMessage(address, string.Empty);
            client.EndMessage();

            client.BeginMessage(address, string.Empty);
            client.EndMessage();

            client.EndBundle();

            Assert.AreEqual(0, client.Sends.Count);

            client.SendAutoBundle();

            Assert.AreEqual(1, client.Sends.Count);
            Assert.AreEqual(4 + 16 + 16 + (2 * (4 + 12)), client.Sends[0].size);
        }

        [Test]
        public void TestSetAutoBundleSendsIfNotEmpty()
        {
            using var client = new TestClient(128)
            {
                AutoBundle = true,
                AutoBundleThreshold = int.MaxValue,
            };
            using var address = new OscAddress("/foo");

            client.BeginBundle();

            client.BeginMessage(address, string.Empty);
            client.EndMessage();

            client.BeginMessage(address, string.Empty);
            client.EndMessage();

            client.EndBundle();

            Assert.AreEqual(0, client.Sends.Count);

            client.AutoBundle = false;

            Assert.AreEqual(1, client.Sends.Count);
            Assert.AreEqual(4 + 16 + 16 + (2 * (4 + 12)), client.Sends[0].size);
        }

        [Test]
        public void TestAutoBundleThreshold()
        {
            using var client = new TestClient(128)
            {
                AutoBundle = true,
            };
            using var address = new OscAddress("/foo");

            client.BeginBundle();

            client.BeginMessage(address, string.Empty);
            client.EndMessage();

            client.BeginMessage(address, string.Empty);
            client.EndMessage();

            Assert.AreEqual(0, client.Sends.Count);

            client.EndBundle();

            Assert.AreEqual(1, client.Sends.Count);
            Assert.AreEqual(4 + 16 + 16 + (2 * (4 + 12)), client.Sends[0].size);
        }

        [Test]
        public void TestAutoBundleDisposed()
        {
            using var client = new TestClient(128);

            client.Dispose();

            Assert.Catch<ObjectDisposedException>(() => _ = client.AutoBundle);
            Assert.Catch<ObjectDisposedException>(() => _ = client.AutoBundleThreshold);
            Assert.Catch<ObjectDisposedException>(() => client.SendAutoBundle());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestEndBundleThrowsWhenNotWriting(bool autoBundle)
        {
            using var client = new TestClient(128)
            {
                AutoBundle = autoBundle,
            };

            Assert.Catch<InvalidOperationException>(() => client.EndBundle());
        }
    }
}
