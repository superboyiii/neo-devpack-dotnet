// Copyright (C) 2015-2025 The Neo Project.
//
// ParameterTransformStrategy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Framework.ContractInvocation.Attributes
{
    /// <summary>
    /// Specifies parameter transformation strategies for custom methods.
    /// </summary>
    public enum ParameterTransformStrategy
    {
        /// <summary>
        /// No parameter transformation.
        /// </summary>
        None,

        /// <summary>
        /// Serialize all parameters into a single byte array.
        /// </summary>
        SerializeToByteArray,

        /// <summary>
        /// Wrap all parameters in a single array parameter.
        /// </summary>
        WrapInArray,

        /// <summary>
        /// Flatten array parameters into individual parameters.
        /// </summary>
        FlattenArrays,

        /// <summary>
        /// Apply custom transformation based on CustomParameterFormat.
        /// </summary>
        Custom
    }
}
