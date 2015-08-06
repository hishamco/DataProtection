// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    public class KeyTests
    {
        [Fact]
        public void Ctor_Properties()
        {
            // Arrange
            var keyId = Guid.NewGuid();
            var creationDate = DateTimeOffset.Now;
            var activationDate = creationDate.AddDays(2);
            var expirationDate = creationDate.AddDays(90);

            // Act
            var key = new Key(keyId, creationDate, activationDate, expirationDate, new Mock<IAuthenticatedEncryptorDescriptor>().Object);

            // Assert
            Assert.Equal(keyId, key.KeyId);
            Assert.Equal(creationDate, key.CreationDate);
            Assert.Equal(activationDate, key.ActivationDate);
            Assert.Equal(expirationDate, key.ExpirationDate);
        }

        [Fact]
        public void SetRevoked_Respected()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var key = new Key(Guid.Empty, now, now, now, new Mock<IAuthenticatedEncryptorDescriptor>().Object);

            // Act & assert
            Assert.False(key.IsRevoked);
            key.SetRevoked();
            Assert.True(key.IsRevoked);
        }

        [Fact]
        public void CreateEncryptorInstance()
        {
            // Arrange
            var expected = new Mock<IAuthenticatedEncryptor>().Object;
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            mockDescriptor.Setup(o => o.CreateEncryptorInstance()).Returns(expected);

            var now = DateTimeOffset.UtcNow;
            var key = new Key(Guid.Empty, now, now, now, mockDescriptor.Object);

            // Act
            var actual = key.CreateEncryptorInstance();

            // Assert
            Assert.Same(expected, actual);
        }
    }
}
