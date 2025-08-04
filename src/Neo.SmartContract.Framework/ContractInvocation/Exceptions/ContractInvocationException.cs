// Copyright (C) 2015-2025 The Neo Project.
//
// ContractInvocationException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.SmartContract.Framework.ContractInvocation.Exceptions
{
    /// <summary>
    /// Base exception class for all contract invocation related errors.
    /// </summary>
    public abstract class ContractInvocationException : Exception
    {
        /// <summary>
        /// Gets the contract identifier associated with this exception.
        /// </summary>
        public string ContractIdentifier { get; }

        /// <summary>
        /// Gets the method name associated with this exception, if applicable.
        /// </summary>
        public string? MethodName { get; }

        /// <summary>
        /// Initializes a new instance of the ContractInvocationException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier</param>
        /// <param name="methodName">The method name, if applicable</param>
        /// <param name="message">The exception message</param>
        protected ContractInvocationException(string contractIdentifier, string? methodName, string message)
            : base(message)
        {
            ContractIdentifier = contractIdentifier ?? throw new ArgumentNullException(nameof(contractIdentifier));
            MethodName = methodName;
        }

        /// <summary>
        /// Initializes a new instance of the ContractInvocationException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier</param>
        /// <param name="methodName">The method name, if applicable</param>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        protected ContractInvocationException(string contractIdentifier, string? methodName, string message, Exception innerException)
            : base(message, innerException)
        {
            ContractIdentifier = contractIdentifier ?? throw new ArgumentNullException(nameof(contractIdentifier));
            MethodName = methodName;
        }
    }

    /// <summary>
    /// Exception thrown when a contract reference cannot be resolved.
    /// </summary>
    public class ContractNotResolvedException : ContractInvocationException
    {
        /// <summary>
        /// Gets additional information about the resolution failure.
        /// </summary>
        public string AdditionalInfo { get; }

        /// <summary>
        /// Initializes a new instance of the ContractNotResolvedException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier that could not be resolved</param>
        /// <param name="additionalInfo">Additional information about the failure</param>
        public ContractNotResolvedException(string contractIdentifier, string additionalInfo = "")
            : base(contractIdentifier, null, $"Contract '{contractIdentifier}' could not be resolved. {additionalInfo}".Trim())
        {
            AdditionalInfo = additionalInfo ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the ContractNotResolvedException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier that could not be resolved</param>
        /// <param name="innerException">The inner exception</param>
        /// <param name="additionalInfo">Additional information about the failure</param>
        public ContractNotResolvedException(string contractIdentifier, Exception innerException, string additionalInfo = "")
            : base(contractIdentifier, null, $"Contract '{contractIdentifier}' could not be resolved. {additionalInfo}".Trim(), innerException)
        {
            AdditionalInfo = additionalInfo ?? string.Empty;
        }
    }

    /// <summary>
    /// Exception thrown when a method is not found in a contract.
    /// </summary>
    public class MethodNotFoundException : ContractInvocationException
    {
        /// <summary>
        /// Gets the list of available methods in the contract.
        /// </summary>
        public string[] AvailableMethods { get; }

        /// <summary>
        /// Initializes a new instance of the MethodNotFoundException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier</param>
        /// <param name="methodName">The method name that was not found</param>
        /// <param name="availableMethods">The list of available methods</param>
        public MethodNotFoundException(string contractIdentifier, string methodName, string[] availableMethods)
            : base(contractIdentifier, methodName,
                   $"Method '{methodName}' not found in contract '{contractIdentifier}'. " +
                   $"Available methods: {string.Join(", ", availableMethods ?? Array.Empty<string>())}")
        {
            AvailableMethods = availableMethods ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Exception thrown when contract compilation fails.
    /// </summary>
    public class ContractCompilationException : ContractInvocationException
    {
        /// <summary>
        /// Gets the project path of the contract that failed to compile.
        /// </summary>
        public string ProjectPath { get; }

        /// <summary>
        /// Gets the compilation errors.
        /// </summary>
        public string[] CompilationErrors { get; }

        /// <summary>
        /// Initializes a new instance of the ContractCompilationException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier</param>
        /// <param name="projectPath">The project path</param>
        /// <param name="errors">The compilation errors</param>
        public ContractCompilationException(string contractIdentifier, string projectPath, string[] errors)
            : base(contractIdentifier, null,
                   $"Failed to compile contract '{contractIdentifier}' at '{projectPath}'. " +
                   $"Errors: {string.Join("; ", errors ?? Array.Empty<string>())}")
        {
            ProjectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
            CompilationErrors = errors ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Exception thrown when contract upgrade validation fails.
    /// </summary>
    public class ContractUpgradeException : ContractInvocationException
    {
        /// <summary>
        /// Gets the validation errors that caused the upgrade to fail.
        /// </summary>
        public string[] ValidationErrors { get; }

        /// <summary>
        /// Initializes a new instance of the ContractUpgradeException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier</param>
        /// <param name="validationErrors">The validation errors</param>
        public ContractUpgradeException(string contractIdentifier, string[] validationErrors)
            : base(contractIdentifier, null,
                   $"Contract upgrade validation failed for '{contractIdentifier}'. " +
                   $"Errors: {string.Join("; ", validationErrors ?? Array.Empty<string>())}")
        {
            ValidationErrors = validationErrors ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Exception thrown when method parameter validation fails.
    /// </summary>
    public class ParameterValidationException : ContractInvocationException
    {
        /// <summary>
        /// Gets the parameter name that failed validation.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Gets the expected parameter type.
        /// </summary>
        public string ExpectedType { get; }

        /// <summary>
        /// Gets the actual parameter type.
        /// </summary>
        public string ActualType { get; }

        /// <summary>
        /// Initializes a new instance of the ParameterValidationException class.
        /// </summary>
        /// <param name="contractIdentifier">The contract identifier</param>
        /// <param name="methodName">The method name</param>
        /// <param name="parameterName">The parameter name</param>
        /// <param name="expectedType">The expected parameter type</param>
        /// <param name="actualType">The actual parameter type</param>
        public ParameterValidationException(string contractIdentifier, string methodName, string parameterName, string expectedType, string actualType)
            : base(contractIdentifier, methodName,
                   $"Parameter validation failed for '{parameterName}' in method '{methodName}' of contract '{contractIdentifier}'. " +
                   $"Expected type: {expectedType}, Actual type: {actualType}")
        {
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
            ExpectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
            ActualType = actualType ?? throw new ArgumentNullException(nameof(actualType));
        }
    }
}
