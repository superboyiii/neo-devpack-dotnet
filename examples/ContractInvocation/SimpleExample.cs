// Simple example demonstrating basic contract invocation patterns
// For more advanced scenarios, see ComprehensiveExample.cs and DynamicInvocationExample.cs

using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.ContractInvocation;
using Neo.SmartContract.Framework.ContractInvocation.Attributes;
using Neo.SmartContract.Framework.ContractInvocation.Exceptions;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Examples.ContractInvocation
{
    [DisplayName("SimpleExample")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Description", "Simple contract invocation example")]
    public class SimpleExample : SmartContract
    {
        #region Contract References

        // Reference to NEO token contract (deployed)
        [ContractReference("NEO",
            PrivnetAddress = "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5",
            TestnetAddress = "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5",
            MainnetAddress = "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5")]
        private static IContractReference? NeoContract;

        // Reference to GAS token contract (deployed)
        [ContractReference("GAS",
            PrivnetAddress = "0xd2a4cff31913016155e38e474a2c06d08be276cf",
            TestnetAddress = "0xd2a4cff31913016155e38e474a2c06d08be276cf",
            MainnetAddress = "0xd2a4cff31913016155e38e474a2c06d08be276cf")]
        private static IContractReference? GasContract;

        // Reference to a custom token that might not be deployed yet
        [ContractReference("MyCustomToken",
            ReferenceType = ContractReferenceType.Development,
            ProjectPath = "../MyToken/MyToken.csproj")]
        private static IContractReference? CustomTokenContract;

        #endregion

        [DisplayName("getTokenInfo")]
        public static object[] GetTokenInfo()
        {
            try
            {
                // Validate that contracts are properly resolved
                if (NeoContract == null || GasContract == null)
                    throw new ContractNotResolvedException("System", "Contract references not initialized");

                if (!NeoContract.IsResolved || !GasContract.IsResolved)
                    throw new ContractNotResolvedException("System", "Contract references not fully resolved");

                // Get contract information safely
                var neoHash = NeoContract.ResolvedHash;
                var gasHash = GasContract.ResolvedHash;

                // Return comprehensive contract information
                return new object[] 
                { 
                    NeoContract.Identifier,
                    neoHash ?? UInt160.Zero,
                    NeoContract.NetworkContext?.CurrentNetwork ?? "unknown",
                    GasContract.Identifier,
                    gasHash ?? UInt160.Zero,
                    GasContract.NetworkContext?.CurrentNetwork ?? "unknown"
                };
            }
            catch (Exception ex)
            {
                // Wrap unexpected exceptions
                throw new ContractNotResolvedException("System", ex, "Failed to get token information");
            }
        }

        [DisplayName("checkContracts")]
        public static bool CheckContracts()
        {
            try
            {
                // Comprehensive contract health check
                return ValidateContract(NeoContract, "NEO") && 
                       ValidateContract(GasContract, "GAS");
            }
            catch
            {
                // Return false on any error for health checks
                return false;
            }
        }

        [DisplayName("getContractBalance")]
        public static BigInteger GetContractBalance(UInt160 account, string tokenType)
        {
            try
            {
                // Input validation
                if (account == null || account == UInt160.Zero)
                    throw new ArgumentException("Invalid account address");

                if (string.IsNullOrEmpty(tokenType))
                    throw new ArgumentException("Token type cannot be empty");

                // Select appropriate contract based on token type
                IContractReference? tokenContract = tokenType switch
                {
                    "NEO" => NeoContract,
                    "GAS" => GasContract,
                    _ => throw new ArgumentException($"Unsupported token type: {tokenType}")
                };

                if (tokenContract == null || !tokenContract.IsResolved)
                    throw new ContractNotResolvedException(tokenType, "Contract not available");

                // Call balanceOf method with proper error handling
                var result = Contract.Call(tokenContract.ResolvedHash!, "balanceOf", CallFlags.ReadOnly, account);
                
                if (result == null)
                    return 0;

                return (BigInteger)result;
            }
            catch (Exception ex)
            {
                throw new ContractNotResolvedException(tokenType, ex, $"Failed to get balance for {tokenType}");
            }
        }

        [DisplayName("batchGetBalances")]
        public static BigInteger[] BatchGetBalances(UInt160 account)
        {
            try
            {
                // Input validation
                if (account == null || account == UInt160.Zero)
                    throw new ArgumentException("Invalid account address");

                var results = new BigInteger[2];
                
                // Get NEO balance
                try
                {
                    results[0] = GetContractBalance(account, "NEO");
                }
                catch (Exception ex)
                {
                    // Log error but continue with other tokens
                    ExecutionEngine.Assert(false, $"Failed to get NEO balance: {ex.Message}");
                    results[0] = 0;
                }

                // Get GAS balance
                try
                {
                    results[1] = GetContractBalance(account, "GAS");
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    ExecutionEngine.Assert(false, $"Failed to get GAS balance: {ex.Message}");
                    results[1] = 0;
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new ContractNotResolvedException("BatchOperation", ex, "Failed to get batch balances");
            }
        }

        #region Handling Undeployed Contracts

        [DisplayName("checkCustomToken")]
        public static object CheckCustomToken()
        {
            try
            {
                // Check if custom token is deployed
                if (CustomTokenContract == null)
                    return "Contract reference not initialized";

                if (!CustomTokenContract.IsResolved)
                {
                    // Contract is not deployed yet
                    return new object[] 
                    { 
                        "Not Deployed",
                        CustomTokenContract.Identifier,
                        CustomTokenContract.NetworkContext?.CurrentNetwork ?? "unknown",
                        false // IsResolved
                    };
                }

                // Contract is deployed, get its info
                var hash = CustomTokenContract.ResolvedHash;
                return new object[]
                {
                    "Deployed",
                    CustomTokenContract.Identifier,
                    hash ?? UInt160.Zero,
                    true // IsResolved
                };
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [DisplayName("callCustomTokenSafely")]
        public static object CallCustomTokenSafely(string method, object[] args)
        {
            try
            {
                // Check if contract is available
                if (CustomTokenContract == null)
                    return "Contract not configured";

                if (!CustomTokenContract.IsResolved)
                {
                    // Log that contract is not deployed
                    Runtime.Log($"Custom token not deployed on {CustomTokenContract.NetworkContext?.CurrentNetwork}");
                    
                    // Return appropriate default based on method
                    return GetDefaultForMethod(method);
                }

                // Make the actual call
                return Contract.Call(CustomTokenContract.ResolvedHash!, method, CallFlags.All, args);
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error calling custom token: {ex.Message}");
                return GetDefaultForMethod(method);
            }
        }

        private static object GetDefaultForMethod(string method)
        {
            // Return sensible defaults for common methods
            return method switch
            {
                "balanceOf" => 0,
                "totalSupply" => 0,
                "decimals" => 8,
                "symbol" => "N/A",
                "transfer" => false,
                _ => null!
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates that a contract reference is properly configured and resolved.
        /// </summary>
        /// <param name="contract">The contract reference to validate</param>
        /// <param name="contractName">The contract name for error reporting</param>
        /// <returns>True if valid, false otherwise</returns>
        private static bool ValidateContract(IContractReference? contract, string contractName)
        {
            if (contract == null)
            {
                ExecutionEngine.Assert(false, $"{contractName} contract reference is null");
                return false;
            }

            if (!contract.IsResolved)
            {
                ExecutionEngine.Assert(false, $"{contractName} contract is not resolved");
                return false;
            }

            if (contract.ResolvedHash == null || contract.ResolvedHash == UInt160.Zero)
            {
                ExecutionEngine.Assert(false, $"{contractName} contract has invalid hash");
                return false;
            }

            return true;
        }

        #endregion
    }
}