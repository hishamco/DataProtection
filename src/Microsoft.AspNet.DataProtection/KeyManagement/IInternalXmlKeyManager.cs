// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    // Used for unit testing
    internal interface IInternalXmlKeyManager
    {
        IKey CreateNewKey(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate);

        IAuthenticatedEncryptorDescriptor DeserializeDescriptorFromKeyElement(XElement keyElement);

        void RevokeSingleKey(Guid keyId, DateTimeOffset revocationDate, string reason);
    }
}
