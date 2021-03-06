// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography.Cng;
using Microsoft.AspNet.Testing.xunit;

namespace Microsoft.AspNet.DataProtection.Test.Shared
{
    public class ConditionalRunTestOnlyOnWindows8OrLaterAttribute : Attribute, ITestCondition
    {
        public bool IsMet => OSVersionUtil.IsWindows8OrLater();

        public string SkipReason { get; } = "Test requires Windows 8 / Windows Server 2012 or higher.";
    }
}
