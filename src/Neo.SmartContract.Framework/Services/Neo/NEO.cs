#pragma warning disable CS0626

using System;
using System.Numerics;

namespace Neo.SmartContract.Framework.Services.Neo
{
    [Contract("0xde5f57d430d3dece511cf975a8d37848cb9e0525")]
    public class NEO
    {
        public static extern string Name();
        public static extern string Symbol();
        public static extern BigInteger Decimals();
        public static extern BigInteger TotalSupply();
        public static extern BigInteger BalanceOf(byte[] account);

        public static extern BigInteger UnclaimedGas(byte[] account, uint end);

        public static extern bool RegisterCandidate(byte[] pubkey);
        public static extern bool UnRegisterCandidate(byte[] pubkey);
        public static extern bool Vote(byte[] account, byte[] voteTo);
        public static extern (string, BigInteger)[] GetCandidates();
        public static extern string[] GetValidators();
        public static extern string[] GetCommittee();
        public static extern string[] GetNextBlockValidators();
    }
}
