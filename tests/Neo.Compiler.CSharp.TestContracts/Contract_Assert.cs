// Copyright (C) 2015-2025 The Neo Project.
//
// Contract_Assert.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Framework;
using System;

namespace Neo.Compiler.CSharp.TestContracts
{
    public class Contract_Assert : SmartContract.Framework.SmartContract
    {
        public int TestAssertFalse()
        {
            int v = 0;
            ExecutionEngine.Assert(true);
            v = 1;
            ExecutionEngine.Assert(false);
            v = 100;
            return v;
        }

        public int TestAssertInFunction()
        {
            int v = 0;
            v = TestAssertFalse();
            v = 1;
            return v;
        }

        public int TestAssertInTry()
        {
            int v = 0;
            try { v = TestAssertFalse(); }
            catch { v = 1; }
            finally { v = 2; }
            return v;
        }

        public int TestAssertInCatch()
        {
            int v = 0;
            try { v = 1; throw new Exception(); }
            catch { v = TestAssertFalse(); }
            finally { v = 2; }
            return v;
        }

        public int TestAssertInFinally()
        {
            int v = 0;
            try { v = 1; }
            catch { v = 2; }
            finally { v = TestAssertFalse(); }
            return v;
        }
    }
}
