// Copyright (C) 2015-2025 The Neo Project.
//
// ContractInvocationIntegrationTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

extern alias scfx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using scfx::Neo.SmartContract.Framework;
using scfx::Neo.SmartContract.Framework.ContractInvocation;
using scfx::Neo.SmartContract.Framework.ContractInvocation.Attributes;
using scfx::Neo.SmartContract.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Framework.UnitTests.ContractInvocation
{
    [TestClass]
    public class ContractInvocationIntegrationTests
    {
        [TestInitialize]
        public void Setup()
        {
            ContractInvocationFactory.ClearRegisteredContracts();
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestEndToEndTokenSwapScenario()
        {
            // Simulate a DEX scenario with multiple token contracts

            // Register token contracts
            var token1 = ContractInvocationFactory.RegisterMultiNetworkContract(
                "Token1",
                CreateMockUInt160(1),
                CreateMockUInt160(2),
                CreateMockUInt160(3),
                "testnet"
            );

            var token2 = ContractInvocationFactory.RegisterMultiNetworkContract(
                "Token2",
                CreateMockUInt160(4),
                CreateMockUInt160(5),
                CreateMockUInt160(6),
                "testnet"
            );

            // Register DEX contract
            var dex = ContractInvocationFactory.RegisterDeployedContract(
                "DexContract",
                CreateMockUInt160(7),
                "testnet"
            );

            // Verify all contracts are registered
            var contracts = ContractInvocationFactory.GetAllRegisteredContracts();
            Assert.AreEqual(3, contracts.Count);

            // Simulate network switch
            ContractInvocationFactory.SwitchNetwork("mainnet");

            // Verify all contracts switched
            Assert.AreEqual("mainnet", token1.NetworkContext.CurrentNetwork);
            Assert.AreEqual("mainnet", token2.NetworkContext.CurrentNetwork);
            Assert.AreEqual("mainnet", dex.NetworkContext.CurrentNetwork);
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestGovernanceVotingScenario()
        {
            // Simulate a governance system with multiple contracts

            // Register governance contracts
            var votingContract = ContractInvocationFactory.RegisterDevelopmentContract(
                "VotingContract", "./voting/VotingContract.csproj"
            );

            var treasuryContract = ContractInvocationFactory.RegisterDeployedContract(
                "TreasuryContract", CreateMockUInt160(8)
            );

            var stakingContract = ContractInvocationFactory.RegisterMultiNetworkContract(
                "StakingContract",
                CreateMockUInt160(9),
                CreateMockUInt160(10),
                CreateMockUInt160(11)
            );

            // Test method resolution is working with contracts
            var voteResolution = MethodResolver.ResolveMethod(votingContract, "vote", new object[] { 1, true });
            Assert.IsNotNull(voteResolution);
            Assert.AreEqual("vote", voteResolution.OriginalMethodName);
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestDeFiProtocolIntegration()
        {
            // Test complex DeFi protocol with multiple interacting contracts

            var contracts = new Dictionary<string, IContractReference>();

            // Core protocol contracts
            contracts["LendingPool"] = ContractInvocationFactory.RegisterDeployedContract(
                "LendingPool", CreateMockUInt160(12)
            );

            contracts["Oracle"] = ContractInvocationFactory.RegisterMultiNetworkContract(
                "PriceOracle",
                CreateMockUInt160(13),
                CreateMockUInt160(14),
                CreateMockUInt160(15)
            );

            contracts["Collateral"] = ContractInvocationFactory.RegisterDevelopmentContract(
                "CollateralManager", "../defi/CollateralManager.csproj"
            );

            // Asset contracts
            var assets = new[] { "USDT", "USDC", "DAI", "WBTC", "WETH" };
            foreach (var asset in assets)
            {
                contracts[asset] = ContractInvocationFactory.RegisterDeployedContract(
                    asset, CreateMockUInt160(asset.GetHashCode())
                );
            }

            // Verify all contracts registered
            var all = ContractInvocationFactory.GetAllRegisteredContracts();
            Assert.AreEqual(8, all.Count); // 3 core + 5 assets

            // Test batch operations
            var assetContracts = all.Where(c => assets.Contains(c.Identifier)).ToList();
            Assert.AreEqual(5, assetContracts.Count);
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestContractUpgradeScenario()
        {
            // Test contract upgrade scenario with address changes

            var contractId = "UpgradableContract";

            // Initial deployment
            var v1Address = CreateMockUInt160(20);
            var contract = ContractInvocationFactory.RegisterDeployedContract(
                contractId, v1Address, "mainnet"
            );

            Assert.AreEqual(v1Address, contract.NetworkContext.GetCurrentNetworkAddress());

            // Simulate upgrade by updating address
            var v2Address = CreateMockUInt160(21);
            contract.NetworkContext.SetNetworkAddress("mainnet", v2Address);

            Assert.AreEqual(v2Address, contract.NetworkContext.GetCurrentNetworkAddress());

            // Add new network after upgrade
            contract.NetworkContext.SetNetworkAddress("testnet", CreateMockUInt160(22));
            Assert.IsTrue(contract.NetworkContext.HasNetworkAddress("testnet"));
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestCrossContractCallChain()
        {
            // Test scenario where contracts call each other in a chain

            // A -> B -> C -> D
            var chainContracts = new System.Collections.Generic.List<IContractReference>();

            for (int i = 0; i < 4; i++)
            {
                var contract = ContractInvocationFactory.RegisterDeployedContract(
                    $"Contract{(char)('A' + i)}",
                    CreateMockUInt160(30 + i)
                );
                chainContracts.Add(contract);
            }

            // Verify chain
            Assert.AreEqual(4, chainContracts.Count);
            foreach (var contract in chainContracts)
            {
                Assert.IsTrue(contract.IsResolved);
            }
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestContractFactoryWithAttributes()
        {
            // Test using attributes for contract configuration
            var attr = new ContractReferenceAttribute("AttributeContract")
            {
                TestnetAddress = "0x" + new string('a', 40),
                ReferenceType = ContractReferenceType.Deployed
            };

            var reference = ContractInvocationFactory.CreateFromAttribute(attr);

            Assert.IsNotNull(reference);
            Assert.AreEqual("AttributeContract", reference.Identifier);
            Assert.IsTrue(reference is DeployedContractReference);
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestMethodResolverWithComplexTypes()
        {
            // Test using the actual static MethodResolver API
            var contract = ContractInvocationFactory.RegisterDeployedContract(
                "ComplexContract", CreateMockUInt160(100), "testnet"
            );

            // Test basic method resolution
            var transferResolution = MethodResolver.ResolveMethod(
                contract, "transfer",
                new object[] { CreateMockUInt160(1), CreateMockUInt160(2), 100 }
            );

            Assert.IsNotNull(transferResolution);
            Assert.AreEqual("transfer", transferResolution.OriginalMethodName);
            Assert.AreEqual("transfer", transferResolution.ResolvedMethodName);
            Assert.AreEqual(3, transferResolution.OriginalParameters?.Length);

            // Test method resolution with null parameters
            var balanceResolution = MethodResolver.ResolveMethod(
                contract, "balanceOf",
                new object[] { CreateMockUInt160(3) }
            );

            Assert.IsNotNull(balanceResolution);
            Assert.AreEqual("balanceOf", balanceResolution.OriginalMethodName);
            Assert.AreEqual(1, balanceResolution.OriginalParameters?.Length);

            // Test method resolution with different parameter types
            var complexResolution = MethodResolver.ResolveMethod(
                contract, "complexMethod",
                new object[] { "string", 123, true, new byte[] { 1, 2, 3 } }
            );

            Assert.IsNotNull(complexResolution);
            Assert.AreEqual("complexMethod", complexResolution.OriginalMethodName);
            Assert.AreEqual(4, complexResolution.OriginalParameters?.Length);
        }

        [TestMethod]
        [Ignore("Framework UInt160 types cannot be created in unit tests")]
        public void TestNetworkMigrationScenario()
        {
            // Test migrating contracts from testnet to mainnet

            var contracts = new[] { "Contract1", "Contract2", "Contract3" };
            var references = new System.Collections.Generic.List<IContractReference>();

            // Deploy on testnet first
            foreach (var name in contracts)
            {
                var testnetAddr = CreateMockUInt160(name.GetHashCode());
                var reference = ContractInvocationFactory.RegisterDeployedContract(
                    name, testnetAddr, "testnet"
                );
                references.Add(reference);
            }

            // Prepare mainnet addresses
            foreach (var reference in references)
            {
                var mainnetAddr = CreateMockUInt160(reference.Identifier.GetHashCode() + 1000);
                reference.NetworkContext.SetNetworkAddress("mainnet", mainnetAddr);
            }

            // Switch to mainnet
            ContractInvocationFactory.SwitchNetwork("mainnet");

            // Verify all contracts have mainnet addresses
            foreach (var reference in references)
            {
                Assert.AreEqual("mainnet", reference.NetworkContext.CurrentNetwork);
                Assert.IsTrue(reference.NetworkContext.HasNetworkAddress("mainnet"));
                Assert.IsNotNull(reference.NetworkContext.GetCurrentNetworkAddress());
            }
        }

        // Helper methods
        private static scfx::Neo.SmartContract.Framework.UInt160 CreateMockUInt160(int seed = 0)
        {
            return scfx::Neo.SmartContract.Framework.UInt160.Zero;
        }

        private static byte[] ConvertToBytes(int value)
        {
            return BitConverter.GetBytes(value);
        }
    }
}
