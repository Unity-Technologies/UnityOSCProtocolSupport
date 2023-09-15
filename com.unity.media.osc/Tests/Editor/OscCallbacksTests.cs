using System;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Media.Osc.Tests.Editor
{
    public class OscCallbacksTests
    {
        [Test]
        public void TestConstructor()
        {
            Assert.DoesNotThrow(() => new OscCallbacks(null, null));

            var readCallback = new Action<OscMessage>((_) => { });
            var mainCallback = new Action(() => { });
            var callbacks = new OscCallbacks(readCallback, mainCallback);

            Assert.AreEqual(readCallback, callbacks.ReadMessage);
            Assert.AreEqual(mainCallback, callbacks.MainThreadQueued);
        }

        [Test]
        public void TestAddCallbacks()
        {
            var read1Triggered = false;
            var read2Triggered = false;
            var main1Triggered = false;
            var main2Triggered = false;

            var callbacks1 = new OscCallbacks((_) =>
                {
                    read1Triggered = true;
                },
                () =>
                {
                    main1Triggered = true;
                });
            var callbacks2 = new OscCallbacks((_) =>
                {
                    read2Triggered = true;
                },
                () =>
                {
                    main2Triggered = true;
                });

            var combinedCallbacks = callbacks1 + callbacks2;

            Assert.AreNotEqual(callbacks1, callbacks2);
            Assert.AreNotEqual(combinedCallbacks, callbacks1);
            Assert.AreNotEqual(combinedCallbacks, callbacks2);

            // check the combined callbacks invokes all the actions
            combinedCallbacks.ReadMessage(null);
            combinedCallbacks.MainThreadQueued();

            Assert.IsTrue(read1Triggered);
            Assert.IsTrue(read2Triggered);
            Assert.IsTrue(main1Triggered);
            Assert.IsTrue(main2Triggered);

            // check the original actions still work independently
            read1Triggered = false;
            read2Triggered = false;
            main1Triggered = false;
            main2Triggered = false;

            callbacks1.ReadMessage(null);
            callbacks1.MainThreadQueued();

            Assert.IsTrue(read1Triggered);
            Assert.IsTrue(main1Triggered);
            Assert.IsFalse(read2Triggered);
            Assert.IsFalse(main2Triggered);
        }

        [Test]
        public void TestRemoveCallbacks()
        {
            var read1Triggered = false;
            var read2Triggered = false;
            var main1Triggered = false;
            var main2Triggered = false;

            var callbacks1 = new OscCallbacks((_) =>
                {
                    read1Triggered = true;
                },
                () =>
                {
                    main1Triggered = true;
                });
            var callbacks2 = new OscCallbacks((_) =>
                {
                    read2Triggered = true;
                },
                () =>
                {
                    main2Triggered = true;
                });

            var combinedCallbacks = callbacks1 + callbacks2;
            var removedCallbacks = combinedCallbacks - callbacks2;

            removedCallbacks.ReadMessage(null);
            removedCallbacks.MainThreadQueued();

            Assert.IsTrue(read1Triggered);
            Assert.IsTrue(main1Triggered);
            Assert.IsFalse(read2Triggered);
            Assert.IsFalse(main2Triggered);
        }
    }
}
