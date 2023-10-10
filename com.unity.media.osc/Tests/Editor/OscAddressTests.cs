using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Media.Osc.Tests.Editor
{
    public unsafe class OscAddressTests
    {
        [Test]
        public void TestStringConstructor()
        {
            const string addressStr = "/root/container/method";
            using var address = new OscAddress(addressStr);
            TestConstructor(address, addressStr, AddressType.Address);
        }

        [Test]
        public void TestCopyConstructor()
        {
            const string addressStr = "/root/container/method";
            using var address1 = new OscAddress(addressStr);
            using var address2 = new OscAddress(address1, Allocator.Persistent);
            TestConstructor(address2, addressStr, AddressType.Address);
        }

        [Test]
        public void TestBufferCopyConstructor()
        {
            const string addressStr = "/root/container/method";

            var buffer = new byte[addressStr.Length];
            Encoding.ASCII.GetBytes(addressStr, 0, addressStr.Length, buffer, 0);

            fixed (byte* bufferPtr = buffer)
            {
                using var address = new OscAddress(bufferPtr, addressStr.Length, Allocator.Persistent);
                TestConstructor(address, addressStr, AddressType.Address);
            }
        }

        [Test]
        public void TestBufferReferenceConstructor()
        {
            const string addressStr = "/root/container/method";

            var buffer = new byte[addressStr.Length + 1];
            Encoding.ASCII.GetBytes(addressStr, 0, addressStr.Length, buffer, 0);
            buffer[addressStr.Length] = 0;

            fixed (byte* bufferPtr = buffer)
            {
                using var address = new OscAddress(bufferPtr, addressStr.Length);
                TestConstructor(address, addressStr, AddressType.Address);
            }
        }

        void TestConstructor(OscAddress address, string expectedAddress, AddressType expectedType)
        {
            Assert.AreEqual(expectedAddress.Length, address.Length);
            for (var i = 0; i < expectedAddress.Length; i++)
            {
                Assert.AreEqual(expectedAddress[i], (char)address.Pointer[i]);
            }

            Assert.AreEqual('\0', address.Pointer[address.Length], "Address string must be followed by a null terminator.");
            Assert.AreEqual(expectedType, address.Type);
        }

        [Test]
        public void TestToString()
        {
            const string addressStr = "/root/container/method";
            using var address = new OscAddress(addressStr);
            Assert.AreEqual(addressStr, address.ToString());
        }

        [Test]
        public void TestEquals()
        {
            const string addressStr = "/root/container/method";
            using var address1 = new OscAddress(addressStr);
            using var address2 = new OscAddress(addressStr);
            Assert.AreEqual(address1, address2);
            Assert.IsTrue(address1 == address2);
        }

        [Test]
        public void TestNotEquals()
        {
            using var address1 = new OscAddress("/root/container/method");
            using var address2 = new OscAddress("/asdf/container/method");
            Assert.AreNotEqual(address1, address2);
            Assert.IsTrue(address1 != address2);
        }

        static IEnumerable TestMatchData
        {
            get
            {
                yield return new TestCaseData("/root/a?", "/root/aa", true);
                yield return new TestCaseData("/root/a?", "/root/ab", true);
                yield return new TestCaseData("/root/a?", "/root/a", false);
                yield return new TestCaseData("/root/a?c/a", "/root/aac/a", true);
                yield return new TestCaseData("/root/a?c/a", "/root/abc/a", true);
                yield return new TestCaseData("/root/a?c/a", "/root/acc/a", true);
                yield return new TestCaseData("/root/?", "/root/a", true);
                yield return new TestCaseData("/root?a", "/root/a", false);

                yield return new TestCaseData("/root/*", "/root/foobar", true);
                yield return new TestCaseData("/root/*", "/root/foobaz", true);
                yield return new TestCaseData("/root/*", "/test/foobar", false);
                yield return new TestCaseData("/root/*", "/root/foobar/a", false);
                yield return new TestCaseData("/root/foobar*", "/root/foobar", true);
                yield return new TestCaseData("/root/*bar/a", "/root/bar/a", true);
                yield return new TestCaseData("/root/*bar/a", "/root/foobar/a", true);
                yield return new TestCaseData("/root/***bar/a", "/root/foobar/a", true);
                yield return new TestCaseData("/root/*bar/a", "/root/foobaz/a", false);
                yield return new TestCaseData("/root/*bar/a", "/root/foobaz", false);
                yield return new TestCaseData("/root/*/*/*/a", "/root/1/2/a", false);
                yield return new TestCaseData("/root/*/*/*/a", "/root/1/2/3/a", true);
                yield return new TestCaseData("/root/*/*/*/a", "/root/1/2/3/4/a", false);
                yield return new TestCaseData("/root/*/2/*/a", "/root/11/2/33/a", true);
                yield return new TestCaseData("/root/*/z/*/a", "/root/1/2/3/a", false);
                yield return new TestCaseData("/root/*/a", "/root/1/2/3/a", false);
                yield return new TestCaseData("/root/*bar/asdf*", "/root/foobar/asdf", true);
                yield return new TestCaseData("/root/*bar/asdf*y", "/root/foobar/asdfqwerty", true);
                yield return new TestCaseData("/root/*bar/asdf*y", "/root/foo/asdfqwerty", false);
                yield return new TestCaseData("/root/*bar/asdf*y", "/root/foobar/asdfqwert", false);

                yield return new TestCaseData("/test//bar", "/test/bar", true);
                yield return new TestCaseData("/test//bar", "/test/part1/bar", true);
                yield return new TestCaseData("/test//bar", "/test/part1/part2/bar", true);
                yield return new TestCaseData("/test//bar", "/test/part1/part2/part3/bar", true);
                yield return new TestCaseData("/test//bar", "/testabar", false);
                yield return new TestCaseData("/test//foo", "/test/foobar", false);
                yield return new TestCaseData("/test//foo", "/test/part1/foobar", false);
                yield return new TestCaseData("/test//bar", "/test/part1/foobar", false);
                yield return new TestCaseData("/test//foo", "/test/part1/part2/foobar", false);

                yield return new TestCaseData("/test/[a]", "/test/a", true);
                yield return new TestCaseData("/test/foo[]ar", "/test/foobar", true);
                yield return new TestCaseData("/test/foo[abc]ar", "/test/fooaar", true);
                yield return new TestCaseData("/test/foo[abc]ar", "/test/foobar", true);
                yield return new TestCaseData("/test/foo[abc]ar", "/test/foocar", true);
                yield return new TestCaseData("/test/foo[abc]ar", "/test/fooAar", false);
                yield return new TestCaseData("/test/foo[abc]ar", "/test/foodar", false);
                yield return new TestCaseData("/test/foo[abc]ar", "/test/foozar", false);
                yield return new TestCaseData("/test/[fF]oo[bB]ar", "/test/foobar", true);
                yield return new TestCaseData("/test/[fF]oo[bB]ar", "/test/FooBar", true);
                yield return new TestCaseData("/test/[fF]oo[bB]ar", "/test/doozar", false);
                yield return new TestCaseData("/test/foo[!]ar", "/test/foobar", true);
                yield return new TestCaseData("/test/foo[!!]ar", "/test/foobar", true);
                yield return new TestCaseData("/test/foo[!!]ar", "/test/foo!ar", false);
                yield return new TestCaseData("/test/foo[!abc]ar", "/test/fooAar", true);
                yield return new TestCaseData("/test/foo[!abc]ar", "/test/foodar", true);
                yield return new TestCaseData("/test/foo[!abc]ar", "/test/foozar", true);
                yield return new TestCaseData("/test/foo[a!bc]ar", "/test/fooaar", true);
                yield return new TestCaseData("/test/foo[ab!c]ar", "/test/foobar", true);
                yield return new TestCaseData("/test/foo[abc!]ar", "/test/foocar", true);
                yield return new TestCaseData("/test/foo[a!bc]ar", "/test/fooAar", false);
                yield return new TestCaseData("/test/foo[ab!c]ar", "/test/foodar", false);
                yield return new TestCaseData("/test/foo[abc!]ar", "/test/foozar", false);
                yield return new TestCaseData("/test/[-]", "/test/-", true);
                yield return new TestCaseData("/test/[!-]", "/test/a", true);
                yield return new TestCaseData("/test/[!-]", "/test/-", false);
                yield return new TestCaseData("/test/[3-7]", "/test/2", false);
                yield return new TestCaseData("/test/[3-7]", "/test/3", true);
                yield return new TestCaseData("/test/[3-7]", "/test/5", true);
                yield return new TestCaseData("/test/[3-7]", "/test/7", true);
                yield return new TestCaseData("/test/[3-7]", "/test/8", false);
                yield return new TestCaseData("/test/[3-7]", "/test/-", false);
                yield return new TestCaseData("/test/[2-35-6]", "/test/1", false);
                yield return new TestCaseData("/test/[2-35-6]", "/test/2", true);
                yield return new TestCaseData("/test/[2-35-6]", "/test/3", true);
                yield return new TestCaseData("/test/[2-35-6]", "/test/4", false);
                yield return new TestCaseData("/test/[2-35-6]", "/test/5", true);
                yield return new TestCaseData("/test/[2-35-6]", "/test/6", true);
                yield return new TestCaseData("/test/[2-35-6]", "/test/7", false);
                yield return new TestCaseData("/test/[2-35-6]", "/test/-", false);
                yield return new TestCaseData("/test/[2-5-7]", "/test/5", true);
                yield return new TestCaseData("/test/[2-5-7]", "/test/6", false);
                yield return new TestCaseData("/test/[2-5-7]", "/test/7", true);
                yield return new TestCaseData("/test/[2-5-7]", "/test/-", true);
                yield return new TestCaseData("/test/[-456]", "/test/3", false);
                yield return new TestCaseData("/test/[-456]", "/test/4", true);
                yield return new TestCaseData("/test/[456-]", "/test/6", true);
                yield return new TestCaseData("/test/[456-]", "/test/7", false);

                yield return new TestCaseData("/test/{}oo", "/test/foo", true);
                yield return new TestCaseData("/test/{}oo", "/test/zoo", true);
                yield return new TestCaseData("/test/a{b,c}d", "/test/abd", true);
                yield return new TestCaseData("/test/a{b,c}d", "/test/acd", true);
                yield return new TestCaseData("/test/a{b,c}d", "/test/aad", false);
                yield return new TestCaseData("/test/a{b,c}d", "/test/add", false);
                yield return new TestCaseData("/test/{foo,bar}", "/test/bar", true);
                yield return new TestCaseData("/test/{foo,bar}", "/test/foo", true);
                yield return new TestCaseData("/test/{foo,bar}", "/test/bar", true);
                yield return new TestCaseData("/test/{foo,bar}", "/test/fo", false);
                yield return new TestCaseData("/test/{foo,bar}", "/test/foz", false);
                yield return new TestCaseData("/test/{foo,bar}", "/test/fooz", false);
                yield return new TestCaseData("/test/{foo,bar}", "/test/baz", false);
                yield return new TestCaseData("/test/{,a1,b2,c3,}", "/test/a1", true);
                yield return new TestCaseData("/test/{,a1,b2,c3,}", "/test/b2", true);
                yield return new TestCaseData("/test/{,a1,b2,c3,}", "/test/c3", true);
                yield return new TestCaseData("/test/{,a1,b2,c3,}", "/test/a2", false);
                yield return new TestCaseData("/test/{,a1,b2,c3,}", "/test/d4", false);

                yield return new TestCaseData("/test//foo*", "/test/part1/part2/foobar", true);
                yield return new TestCaseData("/test//foo*[x-z]{foo,bar}", "/test/part1/part2/foobazbar", true);
            }
        }

        [Test]
        public void TestMatchInvalidAddress()
        {
            using var invalid = new OscAddress("a #");
            using var pattern = new OscAddress("/root/*");
            using var address = new OscAddress("/root/container/method");

            Assert.AreEqual(AddressType.Invalid, invalid.Type);
            Assert.AreEqual(AddressType.Pattern, pattern.Type);
            Assert.AreEqual(AddressType.Address, address.Type);

            Assert.IsFalse(invalid.Matches(pattern));
            Assert.IsFalse(invalid.Matches(address));
        }

        [Test, TestCaseSource(nameof(TestMatchData))]
        public void TestMatch(string patternStr, string addressStr, bool matches)
        {
            using var pattern = new OscAddress(patternStr);
            using var address = new OscAddress(addressStr);

            Assert.AreEqual(AddressType.Pattern, pattern.Type);
            Assert.AreEqual(AddressType.Address, address.Type);

            Assert.IsTrue(pattern.Matches(pattern), "Patterns should always match themselves.");
            Assert.IsTrue(address.Matches(address), "Addresses should always match themselves.");

            if (matches)
            {
                Assert.IsTrue(pattern.Matches(address));
                Assert.IsTrue(address.Matches(pattern));
            }
            else
            {
                Assert.IsFalse(pattern.Matches(address));
                Assert.IsFalse(address.Matches(pattern));
            }
        }
    }
}
