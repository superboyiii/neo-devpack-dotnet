// Copyright (C) 2015-2025 The Neo Project.
//
// ContractReferenceType.cs file belongs to the neo project and is free
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
    /// Specifies the type of contract reference.
    /// </summary>
    public enum ContractReferenceType
    {
        /// <summary>
        /// Automatically determine the reference type based on the identifier and context.
        /// </summary>
        Auto,

        /// <summary>
        /// Reference to a contract under development.
        /// </summary>
        Development,

        /// <summary>
        /// Reference to a deployed contract on the blockchain.
        /// </summary>
        Deployed
    }
}
