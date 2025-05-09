// Copyright (C) 2015-2025 The Neo Project.
//
// TestStorageTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract.Testing.Storage;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.SmartContract.Testing.UnitTests.Storage
{
    [TestClass]
    public class TestStorageTests
    {
        [TestMethod]
        public void TestCheckpoint()
        {
            // Create a new test engine with native contracts already initialized

            var engine = new TestEngine(true);

            // Check that all it works

            Assert.IsTrue(engine.Native.NEO.Storage.Contains(1)); // Prefix_VotersCount
            Assert.AreEqual(100_000_000, engine.Native.NEO.TotalSupply);

            // Create checkpoint

            var checkpoint = engine.Storage.Checkpoint();

            // Create new storage, and restore the checkpoint on it

            var storage = new EngineStorage(new MemoryStore());
            checkpoint.Restore(storage.Snapshot);

            // Create new test engine without initialize
            // and set the storage to the restored one

            engine = new TestEngine(storage, false);

            // Ensure that all works

            Assert.AreEqual(100_000_000, engine.Native.NEO.TotalSupply);

            // Test restoring in raw

            storage = new EngineStorage(new MemoryStore());
            new EngineCheckpoint(new MemoryStream(checkpoint.ToArray())).Restore(storage.Snapshot);

            engine = new TestEngine(storage, false);
            Assert.AreEqual(100_000_000, engine.Native.NEO.TotalSupply);
        }

        [TestMethod]
        public void LoadExportImport()
        {
            EngineStorage store = new(new MemoryStore());

            // empty

            var entries = store.Store.Find([], SeekDirection.Forward).ToArray();
            Assert.AreEqual(0, entries.Length);

            // simple object

            var json = @"{""bXlSYXdLZXk="":""dmFsdWU=""}";

            store.Import(((JObject)JToken.Parse(json)!)!);
            store.Commit();

            entries = store.Store.Find([], SeekDirection.Forward).ToArray();
            Assert.AreEqual(1, entries.Length);

            Assert.AreEqual("myRawKey", Encoding.ASCII.GetString(entries[0].Key));
            Assert.AreEqual("value", Encoding.ASCII.GetString(entries[0].Value));

            // prefix object

            json = @"{""bXk="":{""UmF3S2V5LTI="":""dmFsdWUtMg==""}}";

            store.Import(((JObject)JToken.Parse(json)!)!);
            store.Commit();

            entries = store.Store.Find([], SeekDirection.Forward).ToArray();
            Assert.AreEqual(2, entries.Length);

            Assert.AreEqual("myRawKey", Encoding.ASCII.GetString(entries[0].Key));
            Assert.AreEqual("value", Encoding.ASCII.GetString(entries[0].Value));

            Assert.AreEqual("myRawKey-2", Encoding.ASCII.GetString(entries[1].Key));
            Assert.AreEqual("value-2", Encoding.ASCII.GetString(entries[1].Value));

            // Test import

            EngineStorage storeCopy = new(new MemoryStore());

            store.Commit();
            storeCopy.Import(store.Export());
            storeCopy.Commit();

            entries = storeCopy.Store.Find([], SeekDirection.Forward).ToArray();
            Assert.AreEqual(2, entries.Length);

            Assert.AreEqual("myRawKey", Encoding.ASCII.GetString(entries[0].Key));
            Assert.AreEqual("value", Encoding.ASCII.GetString(entries[0].Value));

            Assert.AreEqual("myRawKey-2", Encoding.ASCII.GetString(entries[1].Key));
            Assert.AreEqual("value-2", Encoding.ASCII.GetString(entries[1].Value));
        }
    }
}
