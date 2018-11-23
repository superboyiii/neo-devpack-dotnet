﻿using System.Numerics;

namespace Neo.SmartContract.Framework.Services.Neo
{
    public static class Storage
    {
        /// <summary>
        /// Returns current StorageContext
        /// </summary>
        public static extern StorageContext CurrentContext
        {
            [Syscall("Neo.Storage.GetContext")]
            get;
        }

        /// <summary>
        /// Returns the byte[] value corresponding to given byte[] key for Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Get")]
        public static extern byte[] Get(StorageContext context, byte[] key);

        /// <summary>
        /// Returns the byte[] value corresponding to given string key for Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Get")]
        public static extern byte[] Get(StorageContext context, string key);

        /// <summary>
        /// Writes byte[] value on byte[] key for given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Put")]
        public static extern void Put(StorageContext context, byte[] key, byte[] value);

        /// <summary>
        /// Writes BigInteger value on byte[] key for given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Put")]
        public static extern void Put(StorageContext context, byte[] key, BigInteger value);

        /// <summary>
        /// Writes string value on byte[] key for given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Put")]
        public static extern void Put(StorageContext context, byte[] key, string value);

        /// <summary>
        /// Writes byte[] value on string key for given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Put")]
        public static extern void Put(StorageContext context, string key, byte[] value);

        /// <summary>
        /// Writes BigInteger value on string key for given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Put")]
        public static extern void Put(StorageContext context, string key, BigInteger value);

        /// <summary>
        /// Writes string value on string key for given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Put")]
        public static extern void Put(StorageContext context, string key, string value);

        /// <summary>
        /// Deletes byte[] key from given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Delete")]
        public static extern void Delete(StorageContext context, byte[] key);

        /// <summary>
        /// Deletes string key from given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Delete")]
        public static extern void Delete(StorageContext context, string key);

        /// <summary>
        /// Returns a byte[] to byte[] iterator for a byte[] prefix on a given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Find")]
        public static extern Iterator<byte[], byte[]> Find(StorageContext context, byte[] prefix);

        /// <summary>
        /// Returns a string to byte[] iterator for a string prefix on a given Storage context (faster: generates opcode directly)
        /// </summary>
        [Syscall("Neo.Storage.Find")]
        public static extern Iterator<string, byte[]> Find(StorageContext context, string prefix);

        /// <summary>
        /// Returns the byte[] value corresponding to given byte[] key for current Storage context
        /// </summary>
        public static byte[] Get(byte[] key)
        {
            return Get(CurrentContext, key);
        }

        /// <summary>
        /// Returns the byte[] value corresponding to given string key for current Storage context
        /// </summary>
        public static byte[] Get(string key)
        {
            return Get(CurrentContext, key);
        }

        /// <summary>
        /// Writes byte[] value on byte[] key for current Storage context
        /// </summary>
        public static void Put(byte[] key, byte[] value)
        {
            Put(CurrentContext, key, value);
        }

        /// <summary>
        /// Writes BignInteger value on byte[] key for current Storage context
        /// </summary>
        public static void Put(byte[] key, BigInteger value)
        {
            Put(CurrentContext, key, value);
        }

        /// <summary>
        /// Writes string value on byte[] key for current Storage context
        /// </summary>
        public static void Put(byte[] key, string value)
        {
            Put(CurrentContext, key, value);
        }

        /// <summary>
        /// Writes byte[] value on string key for current Storage context
        /// </summary>
        public static void Put(string key, byte[] value)
        {
            Put(CurrentContext, key, value);
        }

        /// <summary>
        /// Writes BigInteger value on string key for current Storage context
        /// </summary>
        public static void Put(string key, BigInteger value)
        {
            Put(CurrentContext, key, value);
        }

        /// <summary>
        /// Writes string value on string key for current Storage context
        /// </summary>
        public static void Put(string key, string value)
        {
            Put(CurrentContext, key, value);
        }

        /// <summary>
        /// Deletes byte[] key from current Storage context
        /// </summary>
        public static void Delete(byte[] key)
        {
            Delete(CurrentContext, key);
        }

        /// <summary>
        /// Deletes string key from given Storage context
        /// </summary>
        public static void Delete(string key)
        {
            Delete(CurrentContext, key);
        }

        /// <summary>
        /// Returns a byte[] to byte[] iterator for a byte[] prefix on current Storage context
        /// </summary>
        public static Iterator<byte[], byte[]> Find(byte[] prefix)
        {
            return Find(CurrentContext, prefix);
        }

        /// <summary>
        /// Returns a string to byte[] iterator for a string prefix on current Storage context
        /// </summary>
        public static Iterator<string, byte[]> Find(string prefix)
        {
            return Find(CurrentContext, prefix);
        }
    }
}
