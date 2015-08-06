// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection.Test.Shared;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public class CngGcmAuthenticatedEncryptorDescriptorDeserializerTests
    {
        [ConditionalFact]
        [ConditionalRunTestOnlyOnWindows]
        public void ImportFromXml_CreatesAppropriateDescriptor()
        {
            // Arrange
            var control = new CngGcmAuthenticatedEncryptorDescriptor(
                new CngGcmAuthenticatedEncryptionOptions()
                {
                    EncryptionAlgorithm = Constants.BCRYPT_AES_ALGORITHM,
                    EncryptionAlgorithmKeySize = 192,
                    EncryptionAlgorithmProvider = null
                },
                "k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==".ToSecret()).CreateEncryptorInstance();

            const string xml = @"
                <descriptor version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  <encryption algorithm='AES' keyLength='192' />
                  <masterKey enc:requiresEncryption='true'>k88VrwGLINfVAqzlAp7U4EAjdlmUG17c756McQGdjHU8Ajkfc/A3YOKdqlMcF6dXaIxATED+g2f62wkRRRRRzA==</masterKey>
                </descriptor>";
            var test = new CngGcmAuthenticatedEncryptorDescriptorDeserializer().ImportFromXml(XElement.Parse(xml)).CreateEncryptorInstance();

            // Act & assert
            byte[] plaintext = new byte[] { 1, 2, 3, 4, 5 };
            byte[] aad = new byte[] { 2, 4, 6, 8, 0 };
            byte[] ciphertext = control.Encrypt(new ArraySegment<byte>(plaintext), new ArraySegment<byte>(aad));
            byte[] roundTripPlaintext = test.Decrypt(new ArraySegment<byte>(ciphertext), new ArraySegment<byte>(aad));
            Assert.Equal(plaintext, roundTripPlaintext);
        }
    }
}
