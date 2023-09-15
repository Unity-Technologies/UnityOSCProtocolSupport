using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Media.Osc.Tests.Editor
{
    public class OscUtilsTests
    {
        static IEnumerable TestCharacterIsValidInAddressData
        {
            get
            {
                yield return new TestCaseData('A', true);
                yield return new TestCaseData('a', true);
                yield return new TestCaseData('T', true);
                yield return new TestCaseData('z', true);
                yield return new TestCaseData('/', true);
                yield return new TestCaseData('^', true);
                yield return new TestCaseData('%', true);

                yield return new TestCaseData(' ', false);
                yield return new TestCaseData('#', false);
                yield return new TestCaseData('*', false);
                yield return new TestCaseData('?', false);
                yield return new TestCaseData(',', false);
                yield return new TestCaseData('[', false);
                yield return new TestCaseData(']', false);
                yield return new TestCaseData('{', false);
                yield return new TestCaseData('}', false);

                yield return new TestCaseData('\0', false);
                yield return new TestCaseData('\t', false);
                yield return new TestCaseData('Ø', false);
                yield return new TestCaseData('ば', false);
            }
        }

        [Test, TestCaseSource(nameof(TestCharacterIsValidInAddressData))]
        public void TestCharacterIsValidInAddress(char c, bool valid)
        {
            if (valid)
            {
                Assert.IsTrue(OscUtils.CharacterIsValidInAddress(c));
            }
            else
            {
                Assert.IsFalse(OscUtils.CharacterIsValidInAddress(c));
            }
        }

        static IEnumerable TestCharacterIsValidInAddressPatternData
        {
            get
            {
                yield return new TestCaseData('A', true);
                yield return new TestCaseData('a', true);
                yield return new TestCaseData('T', true);
                yield return new TestCaseData('z', true);
                yield return new TestCaseData('/', true);
                yield return new TestCaseData('^', true);
                yield return new TestCaseData('%', true);

                yield return new TestCaseData(' ', false);
                yield return new TestCaseData('#', false);
                yield return new TestCaseData('*', true);
                yield return new TestCaseData('?', true);
                yield return new TestCaseData(',', true);
                yield return new TestCaseData('[', true);
                yield return new TestCaseData(']', true);
                yield return new TestCaseData('{', true);
                yield return new TestCaseData('}', true);

                yield return new TestCaseData('\0', false);
                yield return new TestCaseData('\t', false);
                yield return new TestCaseData('Ø', false);
                yield return new TestCaseData('ば', false);
            }
        }

        [Test, TestCaseSource(nameof(TestCharacterIsValidInAddressPatternData))]
        public void TestCharacterIsValidInAddressPattern(char c, bool expectedResult)
        {
            if (expectedResult)
            {
                Assert.IsTrue(OscUtils.CharacterIsValidInAddressPattern(c));
            }
            else
            {
                Assert.IsFalse(OscUtils.CharacterIsValidInAddressPattern(c));
            }
        }

        static IEnumerable TestValidateAddressData
        {
            get
            {
                yield return new TestCaseData(AddressType.Invalid, "", "");
                yield return new TestCaseData(AddressType.Address, null, null);

                yield return new TestCaseData(AddressType.Address, "/root/container/a", "/root/container/a");
                yield return new TestCaseData(AddressType.Address, "root/container/a/", "/root/container/a");
                yield return new TestCaseData(AddressType.Address, "root /#container/a/", "/root/container/a");
                yield return new TestCaseData(AddressType.Address, "root\0/containばer/Øa/", "/root/container/a");
                yield return new TestCaseData(AddressType.Address, "root*//[c]ontainer?/{a}/", "/root/container/a");

                yield return new TestCaseData(AddressType.Pattern, "/root/container/a", "/root/container/a");
                yield return new TestCaseData(AddressType.Pattern, "root*//[c]container?/{a}/", "/root*//[c]container?/{a}");
            }
        }

        [Test, TestCaseSource(nameof(TestValidateAddressData))]
        public void TestValidateAddress(AddressType type, string toValidate, string expectedResult)
        {
            OscUtils.ValidateAddress(ref toValidate, type);
            Assert.AreEqual(expectedResult, toValidate);
        }

        static IEnumerable TestGetAddressTypeData
        {
            get
            {
                yield return new TestCaseData(null, AddressType.Invalid);
                yield return new TestCaseData("", AddressType.Invalid);
                yield return new TestCaseData("/", AddressType.Invalid);
                yield return new TestCaseData("asdf", AddressType.Invalid);

                yield return new TestCaseData("/asdf/qwerty", AddressType.Address);
                yield return new TestCaseData("/asdf/QWERTY/1", AddressType.Address);

                yield return new TestCaseData("/asdf/qwerty/", AddressType.Invalid);
                yield return new TestCaseData("/asdf/qwe rty", AddressType.Invalid);
                yield return new TestCaseData("/asdf#/qwerty", AddressType.Invalid);
                yield return new TestCaseData("/asdf/\0", AddressType.Invalid);
                yield return new TestCaseData("/asdf/ば", AddressType.Invalid);

                yield return new TestCaseData("/asdf//qwerty", AddressType.Pattern);
                yield return new TestCaseData("/asdf/qwerty/*", AddressType.Pattern);
                yield return new TestCaseData("/asdf*/qwerty/*", AddressType.Pattern);
                yield return new TestCaseData("/asdf/[qQ]werty", AddressType.Pattern);
                yield return new TestCaseData("/asdf/{foo,bar}", AddressType.Pattern);
                yield return new TestCaseData("//asdf/*/?/[qQ]werty/{foo,bar}", AddressType.Pattern);

                yield return new TestCaseData("/asdf/*/", AddressType.Invalid);
                yield return new TestCaseData("/asdf/[qQwerty/", AddressType.Invalid);
                yield return new TestCaseData("/asdf/[[qQ]]werty/", AddressType.Invalid);
                yield return new TestCaseData("/asdf[/qQ]werty/", AddressType.Invalid);
                yield return new TestCaseData("/asdf/[q*]werty/", AddressType.Invalid);
                yield return new TestCaseData("/asdf/[q?]werty/", AddressType.Invalid);
                yield return new TestCaseData("/asdf/[q{a,b}]werty/", AddressType.Invalid);
                yield return new TestCaseData("/asdf/{{foo,bar}}", AddressType.Invalid);
                yield return new TestCaseData("/asdf/{foo,bar", AddressType.Invalid);
                yield return new TestCaseData("/asdf/foo,bar", AddressType.Invalid);
                yield return new TestCaseData("/asdf{/foo,bar}", AddressType.Invalid);
                yield return new TestCaseData("/asdf/{foo,*}", AddressType.Invalid);
                yield return new TestCaseData("/asdf/{foo,?}", AddressType.Invalid);
                yield return new TestCaseData("/asdf/{foo,[bB]ar}", AddressType.Invalid);
            }
        }

        [Test, TestCaseSource(nameof(TestGetAddressTypeData))]
        public void TestGetAddressType(string address, AddressType expectedResult)
        {
            Assert.AreEqual(expectedResult, OscUtils.GetAddressType(address));
        }

        static IEnumerable TestCreateTagStringData
        {
            get
            {
                yield return new TestCaseData(new TypeTag[] { }, ",");
                yield return new TestCaseData(new TypeTag[] { TypeTag.Float32 }, ",f");
                yield return new TestCaseData(new TypeTag[] { TypeTag.Float32, TypeTag.Float32, TypeTag.Float32 }, ",fff");
                yield return new TestCaseData(new TypeTag[]
                {
                    TypeTag.False,
                    TypeTag.Infinitum,
                    TypeTag.Nil,
                    TypeTag.AltTypeString,
                    TypeTag.True,
                    TypeTag.ArrayStart,
                    TypeTag.ArrayEnd,
                    TypeTag.Blob,
                    TypeTag.AsciiChar32,
                    TypeTag.Float64,
                    TypeTag.Float32,
                    TypeTag.Int64,
                    TypeTag.Int32,
                    TypeTag.MIDI,
                    TypeTag.Color32,
                    TypeTag.String,
                    TypeTag.TimeTag,
                }, ",FINST[]bcdfhimrst");
            }
        }

        [Test]
        public void TestCreateTagStringNull()
        {
            Assert.Catch<ArgumentNullException>(() => OscUtils.CreateTagString(null));
        }

        [Test, TestCaseSource(nameof(TestCreateTagStringData))]
        public void TestCreateTagString(TypeTag[] tags, string expectedResult)
        {
            using var tagString = OscUtils.CreateTagString(tags, Allocator.Temp);

            Assert.AreEqual(expectedResult.Length, tagString.Length);

            for (var i = 0; i < expectedResult.Length; i++)
            {
                Assert.AreEqual(expectedResult[i], (char)tagString[i]);
            }
        }
    }
}
