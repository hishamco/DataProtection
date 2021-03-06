// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNet.DataProtection.Infrastructure;
using Microsoft.AspNet.DataProtection.Abstractions;
using Microsoft.Framework.Internal;

#if DNX451 || DNXCORE50 // [[ISSUE1400]] Replace with DNX_ANY when it becomes available
using Microsoft.Dnx.Runtime;
#endif

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// Helpful extension methods for data protection APIs.
    /// </summary>
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// Creates an <see cref="IDataProtector"/> given a list of purposes.
        /// </summary>
        /// <param name="provider">The <see cref="IDataProtectionProvider"/> from which to generate the purpose chain.</param>
        /// <param name="purposes">The list of purposes which contribute to the purpose chain. This list must
        /// contain at least one element, and it may not contain null elements.</param>
        /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
        /// <remarks>
        /// This is a convenience method which chains together several calls to
        /// <see cref="IDataProtectionProvider.CreateProtector(string)"/>. See that method's
        /// documentation for more information.
        /// </remarks>
        public static IDataProtector CreateProtector([NotNull] this IDataProtectionProvider provider, [NotNull] IEnumerable<string> purposes)
        {
            bool collectionIsEmpty = true;
            IDataProtectionProvider retVal = provider;
            foreach (string purpose in purposes)
            {
                if (purpose == null)
                {
                    throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesCollection, nameof(purposes));
                }
                retVal = retVal.CreateProtector(purpose) ?? CryptoUtil.Fail<IDataProtector>("CreateProtector returned null.");
                collectionIsEmpty = false;
            }

            if (collectionIsEmpty)
            {
                throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesCollection, nameof(purposes));
            }

            Debug.Assert(retVal is IDataProtector); // CreateProtector is supposed to return an instance of this interface
            return (IDataProtector)retVal;
        }

        /// <summary>
        /// Creates an <see cref="IDataProtector"/> given a list of purposes.
        /// </summary>
        /// <param name="provider">The <see cref="IDataProtectionProvider"/> from which to generate the purpose chain.</param>
        /// <param name="purpose">The primary purpose used to create the <see cref="IDataProtector"/>.</param>
        /// <param name="subPurposes">An optional list of secondary purposes which contribute to the purpose chain.
        /// If this list is provided it cannot contain null elements.</param>
        /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
        /// <remarks>
        /// This is a convenience method which chains together several calls to
        /// <see cref="IDataProtectionProvider.CreateProtector(string)"/>. See that method's
        /// documentation for more information.
        /// </remarks>
        public static IDataProtector CreateProtector([NotNull] this IDataProtectionProvider provider, [NotNull] string purpose, params string[] subPurposes)
        {
            // The method signature isn't simply CreateProtector(this IDataProtectionProvider, params string[] purposes)
            // because we don't want the code provider.CreateProtector() [parameterless] to inadvertently compile.
            // The actual signature for this method forces at least one purpose to be provided at the call site.

            IDataProtector protector = provider.CreateProtector(purpose);
            if (subPurposes != null && subPurposes.Length > 0)
            {
                protector = protector?.CreateProtector((IEnumerable<string>)subPurposes);
            }
            return protector ?? CryptoUtil.Fail<IDataProtector>("CreateProtector returned null.");
        }

        /// <summary>
        /// Returns a unique identifier for this application.
        /// </summary>
        /// <param name="services">The application-level <see cref="IServiceProvider"/>.</param>
        /// <returns>A unique application identifier, or null if <paramref name="services"/> is null
        /// or cannot provide a unique application identifier.</returns>
        /// <remarks>
        /// <para>
        /// The returned identifier should be stable for repeated runs of this same application on
        /// this machine. Additionally, the identifier is only unique within the scope of a single
        /// machine, e.g., two different applications on two different machines may return the same
        /// value.
        /// </para>
        /// <para>
        /// This identifier may contain security-sensitive information such as physical file paths,
        /// configuration settings, or other machine-specific information. Callers should take
        /// special care not to disclose this information to untrusted entities.
        /// </para>
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetApplicationUniqueIdentifier(this IServiceProvider services)
        {
            string discriminator = (services?.GetService(typeof(IApplicationDiscriminator)) as IApplicationDiscriminator)?.Discriminator;
#if DNX451 || DNXCORE50 // [[ISSUE1400]] Replace with DNX_ANY when it becomes available
            if (discriminator == null)
            {
                discriminator = (services?.GetService(typeof(IApplicationEnvironment)) as IApplicationEnvironment)?.ApplicationBasePath;
            }
#elif NET451 // do nothing
#else
#error A new target framework was added to project.json, but it's not accounted for in this #ifdef. Please change the #ifdef accordingly.
#endif

            // Remove whitespace and homogenize empty -> null
            discriminator = discriminator?.Trim();
            return (String.IsNullOrEmpty(discriminator)) ? null : discriminator;
        }

        /// <summary>
        /// Retrieves an <see cref="IDataProtectionProvider"/> from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="services">The service provider from which to retrieve the <see cref="IDataProtectionProvider"/>.</param>
        /// <returns>An <see cref="IDataProtectionProvider"/>. This method is guaranteed never to return null.</returns>
        /// <exception cref="InvalidOperationException">If no <see cref="IDataProtectionProvider"/> service exists in <paramref name="services"/>.</exception>
        public static IDataProtectionProvider GetDataProtectionProvider([NotNull] this IServiceProvider services)
        {
            // We have our own implementation of GetRequiredService<T> since we don't want to
            // take a dependency on DependencyInjection.Interfaces.
            IDataProtectionProvider provider = (IDataProtectionProvider)services.GetService(typeof(IDataProtectionProvider));
            if (provider == null)
            {
                throw new InvalidOperationException(Resources.FormatDataProtectionExtensions_NoService(typeof(IDataProtectionProvider).FullName));
            }
            return provider;
        }

        /// <summary>
        /// Retrieves an <see cref="IDataProtector"/> from an <see cref="IServiceProvider"/> given a list of purposes.
        /// </summary>
        /// <param name="services">An <see cref="IServiceProvider"/> which contains the <see cref="IDataProtectionProvider"/>
        /// from which to generate the purpose chain.</param>
        /// <param name="purposes">The list of purposes which contribute to the purpose chain. This list must
        /// contain at least one element, and it may not contain null elements.</param>
        /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
        /// <remarks>
        /// This is a convenience method which calls <see cref="GetDataProtectionProvider(IServiceProvider)"/>
        /// then <see cref="CreateProtector(IDataProtectionProvider, IEnumerable{string})"/>. See those methods'
        /// documentation for more information.
        /// </remarks>
        public static IDataProtector GetDataProtector([NotNull] this IServiceProvider services, [NotNull] IEnumerable<string> purposes)
        {
            return services.GetDataProtectionProvider().CreateProtector(purposes);
        }

        /// <summary>
        /// Retrieves an <see cref="IDataProtector"/> from an <see cref="IServiceProvider"/> given a list of purposes.
        /// </summary>
        /// <param name="services">An <see cref="IServiceProvider"/> which contains the <see cref="IDataProtectionProvider"/>
        /// from which to generate the purpose chain.</param>
        /// <param name="purpose">The primary purpose used to create the <see cref="IDataProtector"/>.</param>
        /// <param name="subPurposes">An optional list of secondary purposes which contribute to the purpose chain.
        /// If this list is provided it cannot contain null elements.</param>
        /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
        /// <remarks>
        /// This is a convenience method which calls <see cref="GetDataProtectionProvider(IServiceProvider)"/>
        /// then <see cref="CreateProtector(IDataProtectionProvider, string, string[])"/>. See those methods'
        /// documentation for more information.
        /// </remarks>
        public static IDataProtector GetDataProtector([NotNull] this IServiceProvider services, [NotNull] string purpose, params string[] subPurposes)
        {
            return services.GetDataProtectionProvider().CreateProtector(purpose, subPurposes);
        }

        /// <summary>
        /// Cryptographically protects a piece of plaintext data.
        /// </summary>
        /// <param name="protector">The data protector to use for this operation.</param>
        /// <param name="plaintext">The plaintext data to protect.</param>
        /// <returns>The protected form of the plaintext data.</returns>
        public static string Protect([NotNull] this IDataProtector protector, [NotNull] string plaintext)
        {
            try
            {
                byte[] plaintextAsBytes = EncodingUtil.SecureUtf8Encoding.GetBytes(plaintext);
                byte[] protectedDataAsBytes = protector.Protect(plaintextAsBytes);
                return WebEncoders.Base64UrlEncode(protectedDataAsBytes);
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // Homogenize exceptions to CryptographicException
                throw Error.CryptCommon_GenericError(ex);
            }
        }

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="protector">The data protector to use for this operation.</param>
        /// <param name="protectedData">The protected data to unprotect.</param>
        /// <returns>The plaintext form of the protected data.</returns>
        /// <exception cref="CryptographicException">
        /// Thrown if <paramref name="protectedData"/> is invalid or malformed.
        /// </exception>
        public static string Unprotect([NotNull] this IDataProtector protector, [NotNull] string protectedData)
        {
            try
            {
                byte[] protectedDataAsBytes = WebEncoders.Base64UrlDecode(protectedData);
                byte[] plaintextAsBytes = protector.Unprotect(protectedDataAsBytes);
                return EncodingUtil.SecureUtf8Encoding.GetString(plaintextAsBytes);
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // Homogenize exceptions to CryptographicException
                throw Error.CryptCommon_GenericError(ex);
            }
        }
    }
}
