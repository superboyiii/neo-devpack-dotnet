using Neo.Cryptography.ECC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract.Testing;

public abstract class Contract_Attribute(Neo.SmartContract.Testing.SmartContractInitialize initialize) : Neo.SmartContract.Testing.SmartContract(initialize), IContractInfo
{
    #region Compiled data

    public static Neo.SmartContract.Manifest.ContractManifest Manifest => Neo.SmartContract.Manifest.ContractManifest.Parse(@"{""name"":""Contract_Attribute"",""groups"":[],""features"":{},""supportedstandards"":[],""abi"":{""methods"":[{""name"":""test"",""parameters"":[],""returntype"":""Boolean"",""offset"":0,""safe"":false},{""name"":""reentrantB"",""parameters"":[],""returntype"":""Void"",""offset"":102,""safe"":false},{""name"":""reentrantA"",""parameters"":[],""returntype"":""Void"",""offset"":250,""safe"":false},{""name"":""reentrantTest"",""parameters"":[{""name"":""value"",""type"":""Integer""}],""returntype"":""Void"",""offset"":294,""safe"":false},{""name"":""_initialize"",""parameters"":[],""returntype"":""Void"",""offset"":463,""safe"":false}],""events"":[]},""permissions"":[{""contract"":""0xacce6fd80d44e1796aa0c2c625e9e4e0ce39efc0"",""methods"":[""base64Decode""]}],""trusts"":[],""extra"":{""nef"":{""optimization"":""All""}}}");

    /// <summary>
    /// Optimization: "All"
    /// </summary>
    public static Neo.SmartContract.NefFile Nef => Neo.IO.Helper.AsSerializable<Neo.SmartContract.NefFile>(Convert.FromBase64String(@"TkVGM1Rlc3RpbmdFbmdpbmUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHA7znO4OTpJcbCoGp54UQN2G/OrAxiYXNlNjREZWNvZGUBAAEPAAD90gFY2CYoCxHADBxBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUE9EU00CGBYNCEIQFcAAnk3AADbMNsoStgkCUrKABQoAzpKeBBR0EVAVwABeBDOQfgn7IwkDgwJZXhjZXB0aW9uOkBZ2CYbCwsSwAwLbm9SZWVudHJhbnQB/wASTTQKYVk0Jlk0X0BXAAN6SngRUdBFQZv2Z855EYhOEFHQUBLASngQUdBFQFcBAXgRzngQzsFFU4tQQZJd6DFwaNgkFAwPQWxyZWFkeSBlbnRlcmVk4BF4Ec54EM7BRVOLUEHmPxiEQFcAAXgRzngQzsFFU4tQQS9Yxe1AWtgmHgsLEsAMC25vUmVlbnRyYW50Af8AEk01dv///2JaNI81Sf///1o0w0BXAAFb2CYdCwsSwAwNcmVlbnRyYW50VGVzdAH/ABJNNBpjWzQ2eBCXJgQiC3gAe5cmBRA0zVs0X0BXAAN6SngRUdBFQZv2Z855EYhOEFHQUBLASngQUdBFQFcBAXgRzngQzsFFU4tQQZJd6DFwaNgkFAwPQWxyZWFkeSBlbnRlcmVk4BF4Ec54EM7BRVOLUEHmPxiEQFcAAXgRzngQzsFFU4tQQS9Yxe1AVgRAolSrOg=="));

    #endregion

    #region Unsafe methods

    /// <summary>
    /// Unsafe method
    /// </summary>
    [DisplayName("reentrantA")]
    public abstract void ReentrantA();

    /// <summary>
    /// Unsafe method
    /// </summary>
    [DisplayName("reentrantB")]
    public abstract void ReentrantB();

    /// <summary>
    /// Unsafe method
    /// </summary>
    [DisplayName("reentrantTest")]
    public abstract void ReentrantTest(BigInteger? value);

    /// <summary>
    /// Unsafe method
    /// </summary>
    [DisplayName("test")]
    public abstract bool? Test();

    #endregion
}
