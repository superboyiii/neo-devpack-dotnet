using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;

namespace Neo.Compiler.MSIL.TestClasses
{
    [Features(ContractFeatures.HasStorage)]
    class Contract_StaticStorageMap : SmartContract.Framework.SmartContract
    {
        private static StorageMap Data = Storage.CurrentContext.CreateMap("data");
        private static readonly StorageMap ReadonlyData = Storage.CurrentContext.CreateMap("readonlydata");

        public static void Put(string message)
        {
            Data.Put(message, 1);
        }

        public static BigInteger Get(string msg)
        {
            return Data.Get(msg)?.ToBigInteger() ?? 0;
        }

        public static void PutReadonly(string message)
        {
            ReadonlyData.Put(message, 2);
        }

        public static BigInteger GetReadonly(string msg)
        {
            return ReadonlyData.Get(msg)?.ToBigInteger() ?? 0;
        }

        public static void Put2(string message)
        {
            var Data2 = Storage.CurrentContext.CreateMap("data");
            Data2.Put(message, 3);
        }

        public static BigInteger Get2(string msg)
        {
            var Data2 = Storage.CurrentContext.CreateMap("data");
            return Data2.Get(msg)?.ToBigInteger() ?? 0;
        }
    }
}
