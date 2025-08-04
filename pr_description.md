## Description

This PR adds a comprehensive contract invocation system that allows developers to invoke other contracts like regular C# classes during development.

## Features

### Core Components
- **Contract Reference Abstractions**: `IContractReference` interface with implementations for development and deployed contracts
- **Network Context Management**: Support for multi-network deployments with different addresses per network  
- **Type-Safe Contract Proxies**: Base classes for creating strongly-typed contract invocation proxies
- **Method Resolution System**: Handles both standard and non-standard contract methods
- **Custom Method Attributes**: Support for parameter transformation and method mapping
- **Development Contract Support**: Invoke contracts that are still under development
- **Blockchain Interface**: Abstraction for querying contract information during compilation

### Key Capabilities

1. **Development Contracts**:
   - Compile-time method validation using source contract reflection
   - Configurable development-time behaviors  
   - Type-safe method calls before contract deployment
   - Automatic compilation dependency resolution

2. **Non-Standard Methods**:
   - Custom method name mapping
   - Parameter transformation and validation
   - Support for complex parameter types
   - Flexible call flag configuration

## Examples

### Example 1: Basic Contract Invocation

```csharp
// Define a proxy for a deployed NEP-17 token contract
public class MyTokenProxy : ContractProxyBase
{
    private static readonly IContractReference TokenContract;
    
    static MyTokenProxy()
    {
        TokenContract = new DeployedContractReference("MyToken");
        TokenContract.NetworkContext.SetNetworkAddress("mainnet", UInt160.Parse("0x1234..."));
    }

    public MyTokenProxy() : base(TokenContract) { }

    public static BigInteger BalanceOf(UInt160 account)
    {
        return (BigInteger)Contract.Call("balanceOf", CallFlags.ReadOnly, account);
    }

    public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data = null)
    {
        return (bool)Contract.Call("transfer", CallFlags.All, from, to, amount, data);
    }
}

// Usage in your contract
public class MyContract : SmartContract
{
    public static BigInteger GetTokenBalance(UInt160 account)
    {
        return MyTokenProxy.BalanceOf(account);
    }
}
```

### Example 2: Multi-Network Contract Support

```csharp
public class CrossNetworkProxy : ContractProxyBase
{
    private static readonly IContractReference CrossContract;
    
    static CrossNetworkProxy()
    {
        // Different addresses for different networks
        CrossContract = DeployedContractReference.CreateMultiNetwork(
            "CrossContract",
            privnetAddress: UInt160.Parse("0xabcd..."),
            testnetAddress: UInt160.Parse("0xefgh..."),
            mainnetAddress: UInt160.Parse("0xijkl..."),
            currentNetwork: "mainnet"
        );
    }

    public CrossNetworkProxy() : base(CrossContract) { }

    public static string CallMethod()
    {
        // Automatically uses correct address based on deployment network
        return (string)Contract.Call("myMethod", CallFlags.ReadOnly);
    }
}
```

### Example 3: Development Contract Invocation

```csharp
// Reference a contract that's still being developed
public class DevContractProxy : DevelopmentContractProxy
{
    private static readonly IContractReference DevContract;
    
    static DevContractProxy()
    {
        DevContract = new DevelopmentContractReference(
            "DevContract", 
            "../DevContract/DevContract.csproj"
        );
    }

    public DevContractProxy() : base(DevContract) { }
    
    // Specify the source contract type for compile-time validation
    protected override Type? SourceContractType => typeof(DevContract);

    public static void Initialize()
    {
        // Call methods on contracts before they're deployed
        Contract.Call("initialize", CallFlags.All);
    }
}
```

### Example 4: Custom Method Mapping

```csharp
public class CustomContractProxy : ContractProxyBase
{
    [ContractReference("CustomContract", "0x5678...")]
    private static IContractReference Contract;

    // Map to a differently named contract method
    [CustomMethod("actualMethodName", CallFlags = CallFlags.All)]
    public static bool DoSomething(string param1, int param2)
    {
        return (bool)Contract.Call("actualMethodName", CallFlags.All, param1, param2);
    }

    // Transform parameters before calling
    [CustomMethod("complexMethod", ParameterTransform = ParameterTransformStrategy.SerializeToByteArray)]
    public static object ComplexCall(ComplexType input)
    {
        // Parameters will be automatically serialized to byte array
        return Contract.Call("complexMethod", CallFlags.All, input);
    }
}
```

### Example 5: Factory Pattern for Contract References

```csharp
public static class ContractFactory
{
    static ContractFactory()
    {
        // Register development contracts
        ContractInvocationFactory.RegisterDevelopmentContract(
            "MyDevContract", 
            "../MyDevContract/MyDevContract.csproj"
        );

        // Register deployed contracts with multi-network support
        ContractInvocationFactory.RegisterMultiNetworkContract(
            "MyDeployedContract",
            privnetAddress: UInt160.Parse("0x1111..."),
            testnetAddress: UInt160.Parse("0x2222..."),
            mainnetAddress: UInt160.Parse("0x3333..."),
            currentNetwork: "testnet"
        );
    }

    public static IContractReference GetContract(string identifier)
    {
        return ContractInvocationFactory.GetContractReference(identifier);
    }
}
```

### Example 6: Dynamic Method Resolution

```csharp
public class DynamicProxy : ContractProxyBase
{
    private static readonly IContractReference Contract;
    
    static DynamicProxy()
    {
        Contract = new DeployedContractReference("DynamicContract");
        Contract.NetworkContext.SetNetworkAddress("mainnet", UInt160.Parse("0x9999..."));
    }

    public DynamicProxy() : base(Contract) { }

    public static object DynamicCall(string method, params object[] args)
    {
        // Use the static MethodResolver to resolve methods dynamically
        var resolution = MethodResolver.ResolveMethod(Contract, method, args);
        
        return Contract.Call(
            resolution.ResolvedMethodName, 
            resolution.CallFlags, 
            resolution.ResolvedParameters
        );
    }

    // Custom method with attribute-based configuration
    [CustomMethod("performOperation", CallFlags = CallFlags.All)]
    public static object CustomOp(string param1, int param2)
    {
        // The CustomMethod attribute handles the mapping
        return Contract.Call("performOperation", CallFlags.All, param1, param2);
    }
}
```

## Implementation

The system includes:
- 14 new source files in the ContractInvocation namespace
- Comprehensive unit tests covering all scenarios
- Full documentation for developers

## Testing

All tests pass locally. The contract invocation system has been thoroughly tested with:
- Development contract scenarios
- Deployed contract scenarios
- Standard method calls
- Non-standard method calls with custom attributes
- Parameter transformation strategies
- Multi-network support

## Breaking Changes

None. This is a new feature that doesn't affect existing functionality.

## Documentation

- [CONTRACT_INVOCATION.md](docs/CONTRACT_INVOCATION.md) - Basic usage guide
- [CONTRACT_INVOCATION_ADVANCED.md](docs/CONTRACT_INVOCATION_ADVANCED.md) - Advanced scenarios
