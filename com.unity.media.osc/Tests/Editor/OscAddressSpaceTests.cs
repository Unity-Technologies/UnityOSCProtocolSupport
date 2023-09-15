using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Media.Osc.Tests.Editor
{
    public class OscAddressSpaceTests
    {
        [Test]
        public void TestAddCallbackAddress()
        {
            using var addressSpace = new OscAddressSpace();
            using var address = new OscAddress("/root/container/method");
            var callbacks = new OscCallbacks((message) => { });

            Assert.IsFalse(addressSpace.TryAddCallback(address, null));
            Assert.IsFalse(addressSpace.TryAddCallback(default(OscAddress), callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(address, callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(address, callbacks));
        }

        [Test]
        public void TestAddCallbackAddressString()
        {
            using var addressSpace = new OscAddressSpace();
            var address = "/root/container/method";
            var callbacks = new OscCallbacks((message) => { });

            Assert.IsFalse(addressSpace.TryAddCallback(address, null));
            Assert.IsFalse(addressSpace.TryAddCallback(null, callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(address, callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(address, callbacks));
        }
        [Test]
        public void TestAddCallbackPattern()
        {
            using var addressSpace = new OscAddressSpace();
            using var pattern = new OscAddress("/root/container/method?");
            var callbacks = new OscCallbacks((message) => { });

            Assert.IsFalse(addressSpace.TryAddCallback(pattern, null));
            Assert.IsFalse(addressSpace.TryAddCallback(default(OscAddress), callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(pattern, callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(pattern, callbacks));
        }

        [Test]
        public void TestAddCallbackPatternString()
        {
            using var addressSpace = new OscAddressSpace();
            var pattern = "/root/container/method?";
            var callbacks = new OscCallbacks((message) => { });

            Assert.IsFalse(addressSpace.TryAddCallback(pattern, null));
            Assert.IsFalse(addressSpace.TryAddCallback(null, callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(pattern, callbacks));
            Assert.IsTrue(addressSpace.TryAddCallback(pattern, callbacks));
        }

        [Test]
        public void TestRemoveCallbackAddress()
        {
            using var addressSpace = new OscAddressSpace();
            using var address = new OscAddress("/root/container/method");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address, callbacks1);
            addressSpace.TryAddCallback(address, callbacks2);

            Assert.IsFalse(addressSpace.RemoveCallback(address, null));
            Assert.IsFalse(addressSpace.RemoveCallback(default(OscAddress), callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(address, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks2));
        }

        [Test]
        public void TestRemoveCallbackAddressString()
        {
            using var addressSpace = new OscAddressSpace();
            var address = "/root/container/method";
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address, callbacks1);
            addressSpace.TryAddCallback(address, callbacks2);

            Assert.IsFalse(addressSpace.RemoveCallback(address, null));
            Assert.IsFalse(addressSpace.RemoveCallback(null, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(address, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks2));
        }

        [Test]
        public void TestRemoveCallbackPattern()
        {
            using var addressSpace = new OscAddressSpace();
            using var pattern = new OscAddress("/root/container/method?");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(pattern, callbacks1);
            addressSpace.TryAddCallback(pattern, callbacks2);

            Assert.IsFalse(addressSpace.RemoveCallback(pattern, null));
            Assert.IsFalse(addressSpace.RemoveCallback(default(OscAddress), callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(pattern, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks2));
        }

        [Test]
        public void TestRemoveCallbackPatternString()
        {
            using var addressSpace = new OscAddressSpace();
            var pattern = "/root/container/method?";
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(pattern, callbacks1);
            addressSpace.TryAddCallback(pattern, callbacks2);

            Assert.IsFalse(addressSpace.RemoveCallback(pattern, null));
            Assert.IsFalse(addressSpace.RemoveCallback(null, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(pattern, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks2));
        }

        [Test]
        public void TestRemoveCallbackMultipleAddresses()
        {
            using var addressSpace = new OscAddressSpace();
            using var address1 = new OscAddress("/root/container/method1");
            using var address2 = new OscAddress("/root/container/method2");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address1, callbacks1);
            addressSpace.TryAddCallback(address1, callbacks2);
            addressSpace.TryAddCallback(address2, callbacks1);
            addressSpace.TryAddCallback(address2, callbacks2);

            Assert.IsFalse(addressSpace.RemoveCallback(address1, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(address1, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(address1, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(address1, callbacks2));

            Assert.IsFalse(addressSpace.RemoveCallback(address2, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(address2, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(address2, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(address2, callbacks2));
        }

        [Test]
        public void TestRemoveCallbackMultiplePatterns()
        {
            using var addressSpace = new OscAddressSpace();
            using var pattern1 = new OscAddress("/root/container/method1?");
            using var pattern2 = new OscAddress("/root/container/method2?");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(pattern1, callbacks1);
            addressSpace.TryAddCallback(pattern1, callbacks2);
            addressSpace.TryAddCallback(pattern2, callbacks1);
            addressSpace.TryAddCallback(pattern2, callbacks2);

            Assert.IsFalse(addressSpace.RemoveCallback(pattern1, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern1, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(pattern1, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern1, callbacks2));

            Assert.IsFalse(addressSpace.RemoveCallback(pattern2, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern2, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(pattern2, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern2, callbacks2));
        }

        [Test]
        public void TestRemoveCallbackAddressAndPattern()
        {
            using var addressSpace = new OscAddressSpace();
            using var address = new OscAddress("/root/container/method1");
            using var pattern = new OscAddress("/root/container/method?");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address, callbacks1);
            addressSpace.TryAddCallback(address, callbacks2);
            addressSpace.TryAddCallback(pattern, callbacks1);
            addressSpace.TryAddCallback(pattern, callbacks2);

            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(address, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(address, callbacks2));

            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks1));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks1));
            Assert.IsTrue(addressSpace.RemoveCallback(pattern, callbacks2));
            Assert.IsFalse(addressSpace.RemoveCallback(pattern, callbacks2));
        }

        [Test]
        public void TestRemoveAllCallbacksAddress()
        {
            using var addressSpace = new OscAddressSpace();
            using var address = new OscAddress("/root/container/method");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address, callbacks1);
            addressSpace.TryAddCallback(address, callbacks2);

            Assert.IsFalse(addressSpace.RemoveAllCallbacks(default(OscAddress)));
            Assert.IsTrue(addressSpace.RemoveAllCallbacks(address));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(address));
        }

        [Test]
        public void TestRemoveAllCallbacksAddressString()
        {
            using var addressSpace = new OscAddressSpace();
            var address = "/root/container/method";
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address, callbacks1);
            addressSpace.TryAddCallback(address, callbacks2);

            Assert.IsFalse(addressSpace.RemoveAllCallbacks(default(OscAddress)));
            Assert.IsTrue(addressSpace.RemoveAllCallbacks(address));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(address));
        }

        [Test]
        public void TestRemoveAllCallbacksPattern()
        {
            using var addressSpace = new OscAddressSpace();
            using var pattern = new OscAddress("/root/container/method?");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(pattern, callbacks1);
            addressSpace.TryAddCallback(pattern, callbacks2);

            Assert.IsFalse(addressSpace.RemoveAllCallbacks(default(OscAddress)));
            Assert.IsTrue(addressSpace.RemoveAllCallbacks(pattern));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(pattern));
        }

        [Test]
        public void TestRemoveAllCallbacksPatternString()
        {
            using var addressSpace = new OscAddressSpace();
            var pattern = "/root/container/method?";
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(pattern, callbacks1);
            addressSpace.TryAddCallback(pattern, callbacks2);

            Assert.IsFalse(addressSpace.RemoveAllCallbacks(default(OscAddress)));
            Assert.IsTrue(addressSpace.RemoveAllCallbacks(pattern));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(pattern));
        }

        [Test]
        public void TestRemoveAllCallbackMultipleAddresses()
        {
            using var addressSpace = new OscAddressSpace();
            using var address1 = new OscAddress("/root/container/method1");
            using var address2 = new OscAddress("/root/container/method2");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address1, callbacks1);
            addressSpace.TryAddCallback(address1, callbacks2);
            addressSpace.TryAddCallback(address2, callbacks1);
            addressSpace.TryAddCallback(address2, callbacks2);

            Assert.IsTrue(addressSpace.RemoveAllCallbacks(address1));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(address1));

            Assert.IsTrue(addressSpace.RemoveAllCallbacks(address2));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(address2));
        }

        [Test]
        public void TestRemoveAllCallbackAddressAndPattern()
        {
            using var addressSpace = new OscAddressSpace();
            using var address = new OscAddress("/root/container/method1");
            using var pattern = new OscAddress("/root/container/method?");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address, callbacks1);
            addressSpace.TryAddCallback(address, callbacks2);
            addressSpace.TryAddCallback(pattern, callbacks1);
            addressSpace.TryAddCallback(pattern, callbacks2);

            Assert.IsTrue(addressSpace.RemoveAllCallbacks(address));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(address));

            Assert.IsTrue(addressSpace.RemoveAllCallbacks(pattern));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(pattern));
        }

        [Test]
        public void TestClear()
        {
            using var addressSpace = new OscAddressSpace();
            var address = "/root/container/method1";
            var pattern = "/root/container/method?";
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address, callbacks1);
            addressSpace.TryAddCallback(address, callbacks2);
            addressSpace.TryAddCallback(pattern, callbacks1);
            addressSpace.TryAddCallback(pattern, callbacks2);

            addressSpace.Clear();

            Assert.IsFalse(addressSpace.RemoveAllCallbacks(address));
            Assert.IsFalse(addressSpace.RemoveAllCallbacks(pattern));
        }

        [Test]
        public void TestFindMatchingCallbacksAddress()
        {
            using var addressSpace = new OscAddressSpace();
            using var address1 = new OscAddress("/root/container/method1");
            using var address2 = new OscAddress("/root/container/method2");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address1, callbacks1);
            addressSpace.TryAddCallback(address2, callbacks2);

            var callbacks = new List<OscCallbacks>();

            addressSpace.FindMatchingCallbacks(address1, callbacks);

            Assert.AreEqual(1, callbacks.Count);
        }

        [Test]
        public void TestFindMatchingCallbacksPattern()
        {
            using var addressSpace = new OscAddressSpace();
            using var pattern1 = new OscAddress("/root/container/method1?");
            using var pattern2 = new OscAddress("/root/container/method2?");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(pattern1, callbacks1);
            addressSpace.TryAddCallback(pattern2, callbacks2);

            var callbacks = new List<OscCallbacks>();

            addressSpace.FindMatchingCallbacks(pattern1, callbacks);

            Assert.AreEqual(1, callbacks.Count);
        }

        [Test]
        public void TestFindMatchingCallbacksAddressesAndPatterns()
        {
            using var addressSpace = new OscAddressSpace();
            using var address1 = new OscAddress("/root/container/method1");
            using var address2 = new OscAddress("/root/container/method2");
            var callbacks1 = new OscCallbacks((message) => { });
            var callbacks2 = new OscCallbacks((message) => { });

            addressSpace.TryAddCallback(address1, callbacks1);
            addressSpace.TryAddCallback(address1, callbacks2);
            addressSpace.TryAddCallback(address2, callbacks1);
            addressSpace.TryAddCallback(address2, callbacks2);

            using var pattern1 = new OscAddress("/root/container/method?");
            using var pattern2 = new OscAddress("/root/container/methodAlt?");
            var callbacks = new List<OscCallbacks>();

            addressSpace.FindMatchingCallbacks(address1, callbacks);

            Assert.AreEqual(1, callbacks.Count);

            addressSpace.FindMatchingCallbacks(address2, callbacks);

            Assert.AreEqual(1, callbacks.Count);

            addressSpace.FindMatchingCallbacks(pattern1, callbacks);

            Assert.AreEqual(2, callbacks.Count);

            addressSpace.FindMatchingCallbacks(pattern2, callbacks);

            Assert.AreEqual(0, callbacks.Count);
        }
    }
}
