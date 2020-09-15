using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace $safeprojectname$
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is a contract example")]
    [Features(ContractFeatures.HasStorage)]
    public class Contract1 : SmartContract
    {
        //TODO: Replace it with your own address.
        static readonly byte[] Owner = "NiNmXL8FjEUEs1nfX9uHFBNaenxDHJtmuB".ToScriptHash();

        private static bool IsOwner() => Runtime.CheckWitness(Owner);

        // When this contract address is included in the transaction signature,
        // this method will be triggered as a VerificationTrigger to verify that the signature is correct.
        // For example, this method needs to be called when withdrawing token from the contract.
        public static bool Verify() => IsOwner();

        // TODO: Replace it with your methods.
        public static bool MyMethod()
        {
            Storage.Put("Hello", "World");
            return true;
        }

        public static void Update(byte[] script, string manifest)
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            Contract.Update(script, manifest);
        }

        public static void Destroy()
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            Contract.Destroy();
        }
    }
}
