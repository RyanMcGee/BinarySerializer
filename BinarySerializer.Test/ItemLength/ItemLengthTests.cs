﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinarySerializer.Test.ItemLength
{
    [TestClass]
    public class ItemLengthTests : TestBase
    {
        [TestMethod]
        public void ItemConstLengthTest()
        {
            var expected = new ItemConstLengthClass {List = new List<string>(new[] {"abc", "def", "ghi"})};
            var actual = Roundtrip(expected, expected.List.Count*3);
            Assert.IsTrue(expected.List.SequenceEqual(actual.List));
        }

        [TestMethod]
        public void ItemBoundLengthTest()
        {
            var expected = new ItemBoundLengthClass { List = new List<string>(new[] { "abc", "def", "ghi" }) };
            var actual = Roundtrip(expected, 4 + expected.List.Count * 3);
            Assert.IsTrue(expected.List.SequenceEqual(actual.List));
        }

        [TestMethod]
        public void ItemLengthListOfByteArraysTest()
        {
            var expected = new ItemLengthListOfByteArrayClass
            {
                Arrays = new List<byte[]> {new byte[3], new byte[3], new byte[3]}
            };

            var actual = Roundtrip(expected);

            Assert.AreEqual(expected.Arrays.Count, actual.Arrays.Count);
        }
    }
}
