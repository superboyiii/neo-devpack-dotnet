// Copyright (C) 2015-2025 The Neo Project.
//
// StaticStorageMapTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Testing;
using Neo.SmartContract.Testing.Exceptions;
using System;

namespace Neo.SmartContract.Framework.UnitTests.Services
{
    [TestClass]
    public class StaticStorageMapTest : DebugAndTestBase<Contract_StaticStorageMap>
    {
        [TestMethod]
        public void Test_Storage()
        {
            Contract.Put2("a");
            Assert.AreEqual(3, Contract.Get2("a"));
        }

        [TestMethod]
        public void Test_StaticStorageMap()
        {
            Contract.Put("a");
            Assert.AreEqual(1, Contract.Get("a"));
            Contract.PutReadonly("a");
            Assert.AreEqual(2, Contract.GetReadonly("a"));
        }

        [TestMethod]
        public void Test_StaticStorageMapBytePrefix()
        {
            Contract.Teststoragemap_Putbyteprefix(0);
            Assert.AreEqual(123, Contract.Teststoragemap_Getbyteprefix(0));

            Contract.Teststoragemap_Putbyteprefix(255);
            Assert.AreEqual(123, Contract.Teststoragemap_Getbyteprefix(255));

            Contract.Teststoragemap_Putbyteprefix(-128);
            Assert.AreEqual(123, Contract.Teststoragemap_Getbyteprefix(-128));

            Contract.Teststoragemap_Putbyteprefix(127);
            Assert.AreEqual(123, Contract.Teststoragemap_Getbyteprefix(127));

            var exception = Assert.ThrowsException<TestException>(() => Contract.Teststoragemap_Putbyteprefix(256));
            Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        }
    }
}
