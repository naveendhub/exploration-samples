#region (c) Koninklijke Philips N.V. 2018
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written permission of the copyright owner.
//
// Filename: AssemblyResolverFinder.cs
//
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;


#if !(BUILD_TESTFRAMEWORK || SII_ASSEMBLY)
using Philips.Platform.CommonUtilities.AssemblyResolution;
#endif

// TODO: replace reflection only approach by using direct type references.
// Currently we have some assembly load issues, due to mixed context assembly loading.
// To fully use the same approach as in utilities we need to move the TestFW to common.

namespace Philips.Platform.CommonUtilities.AssemblyResolutionFind {

    /// <summary>
    /// Very simple assembly resolver. Sole purpose: Find and load the
    /// Philips.Platform.CommonUtilities.dll that has the full-feature AssemblyResolver.
    /// </summary>
    /// <remarks>
    /// This file is copied to Common\Output\Shared\AssemblyResolution
    /// for source access by other parts of the platform and to be published to BIU developers.
    ///
    /// This file shall be included as source-reference or source-copy in each .exe
    /// that wants to use the platform and thus needs to active the full-feature AssemblyResolver.
    ///
    /// Intended use:
    /// 1) AssemblyResolverFinder.AddSearchPath(PathToPlatform);   [not needed inside platform]
    /// 2) AssemblyResolverFinder.Find();
    /// 3) use the full-feature AssemblyResolver, e.g.
    /// 3a) any desired AssemblyResolver settings
    /// 3b) AssemblyResolver.Start();
    /// 4) Regular platform usage
    ///
    /// Be sure to have calls after step 2 in DIFFERENT method(s) that are
    /// isolated with [MethodImpl(MethodImplOptions.NoInlining)]
    ///
    /// Note: this class has specific entry points for testing.
    /// These will NOT work in production deployments
    /// </remarks>
    internal static class AssemblyResolverFinder {
        private static readonly IList<string> searchPaths = new List<string>();
        private static Type assemblyResolver;

        /// <summary>
        /// Add additional search path for finding Philips.Platform.CommonUtilities.dll
        /// </summary>
        /// <remarks>
        /// Not needed for platform code that is in 'regular' locations of the platform itself.
        /// </remarks>
        /// <param name="path">Fully qualified (root) searchpath.</param>
        internal static void AddSearchPath(string path) {
            searchPaths.Add(path);
        }

        /// <summary>
        /// Call this method to find/load the Philips.Platform.CommonUtilities.dll.
        /// If needed, first call AddSearchPath.
        /// </summary>
        /// <remarks>
        /// After loading the Philips.Platform.CommonUtilities.dll,
        /// the full-feature AssemblyResolver is available.
        /// </remarks>
        /// <returns>The assembly resolver type or null on failure</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Type Find() {
            if (assemblyResolver is null) {
                AddDefaultPlatformPaths();
                // Register for assembly resolver event
                AppDomain appDomain = AppDomain.CurrentDomain;
                appDomain.AssemblyResolve += OnAssemblyResolve;
                assemblyResolver = TriggerAccess();
                appDomain.AssemblyResolve -= OnAssemblyResolve;
            }
            return assemblyResolver;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Type TriggerAccess() {
            
            #if !(BUILD_TESTFRAMEWORK || SII_ASSEMBLY)

            return typeof(AssemblyResolver);

            #else

            // Use reflection because testframework and SII cannot refer CommonUtilities
            Type result = null;
            var commonUtilAssembly = Assembly.Load("Philips.Platform.CommonUtilities");
            if (commonUtilAssembly != null) {

                #if SII_ASSEMBLY
                CommonUtilitiesLocation = commonUtilAssembly.Location;
                #endif

                result =
                    commonUtilAssembly.GetType(
                        "Philips.Platform.CommonUtilities.AssemblyResolution.AssemblyResolver"
                    );
            }
            return result;

            #endif
        }

        #if SII_ASSEMBLY
        internal static string CommonUtilitiesLocation { get; private set; }
        #endif

        /// <summary>
        /// Add assembly resolve paths due to platform archive structure.
        /// </summary>
        private static void AddDefaultPlatformPaths() {
            // The default knowledge starts with our build/deploy choices
            // - Always have the executable directory
            // - Add the ..\bin parent, because .exe is typically in ...\Bin\x86 or ...\Bin\X64
            string myLocation = Assembly.GetExecutingAssembly().OriginalDirectoryLocation();
            if (myLocation == null) {
                // Shall not happen. Make TICS happy.
                throw new InvalidOperationException("Could not detect my location");
            }
            string parentDir = Path.GetDirectoryName(myLocation);
            if (
                !string.IsNullOrEmpty(parentDir) &&
                parentDir.EndsWith(@"\Bin", StringComparison.OrdinalIgnoreCase)
            ) {
                AddSearchPath(parentDir);
            }
            AddSearchPath(myLocation);

            // In a developer-build environment, the binaries are split over archive sections.
            // Add all to the list.
            // Assume potentially more variations in caller locations than only system.
            bool developerEnvironment =
                myLocation.IndexOf(@"\Output\Bin", StringComparison.OrdinalIgnoreCase) >= 0;
            if (developerEnvironment) {
                string archiveRoot = FindArchiveRoot(new DirectoryInfo(myLocation));
                if (!string.IsNullOrEmpty(archiveRoot)) {
                    // this is the developer-build environment
                    // we should only need common to enable assembly resolution
                    AddSearchPath(Path.Combine(archiveRoot, @"Common\Output\Bin\"));
                }
            }
        }

        /// <summary>
        /// Start assembly resolver from the test framework on behalf of a referencing test
        /// this is used as part of test bootstrap process.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification =
                "This code can fail in many ways due to reflection. " +
                "No exceptions are allowed to come out of this method."
        )]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Generated/template code"
        )]
        internal static void StartAssemblyResolverForTestFramework(Assembly assembly) {
            // The provided assembly provides the name of the test assembly which would be local
            // to the test assembly resolvers configuration
            try {
                var resolverType = Find();
                if (resolverType is null) {
                    throw new TypeLoadException("Could not load AssemblyResolver from CommonTypes");
                }
                var startMethod = resolverType.GetMethod("StartForTestFramework");
                if (startMethod is null) {
                    throw new MissingMethodException(
                        "AssemblyResolver does not provide StartForTestFramework method"
                    );
                }
                startMethod.Invoke(null, new object[] { assembly });
            } catch(Exception e) {
                Console.WriteLine(
                    "Failed to start assembly resolution issue: " +
                    e.GetType().Name + " " + e.Message
                );
                Console.WriteLine(
                    "Failed to start assembly resolution stack: " +  e.StackTrace
                );
                throw;
            }
        }

        /// <summary>
        /// Start assembly resolver from provided test assembly.
        /// This method is ONLY needed for very specific test usage.
        /// e.g. ensure to have assembly resolution for tests available in a new appdomain
        /// When in doubt, do not use.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification =
                "This code can fail in many ways due to reflection. " +
                "No exceptions are allowed to come out of this method."
        )]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Generated/template code"
        )]
        internal static void StartAssemblyResolverForTest() {
            StartAssemblyResolverForTestFramework(typeof(AssemblyResolverFinder).Assembly);
        }

        /// <summary>
        /// Start assembly resolver ONLY for the provided limited set of folders,
        /// no default folders are added.
        /// This is used in specific test scenarios where (ONLY IN DEVELOPER BUILD)
        /// we can limit the search folders.
        /// </summary>
        /// <remarks>
        /// Any other assembly resolvers are still enabled, so effect of this is marginally and
        /// cannot be relied upon.
        /// If this behavior is used, the actual test assembly should contain additional measures
        /// to validate the intended behavior
        /// </remarks>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification =
                "This code can fail in many ways due to reflection. " +
                "No exceptions are allowed to come out of this method."
        )]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Generated/template code"
        )]
        internal static void StartLimitedAssemblyResolver(string[] folders) {
            // The provided assembly provides the name of the test assembly which would be local
            // to the test assembly resolvers configuration
            try {
                var resolverType = Find();
                if (resolverType is null) {
                    throw new TypeLoadException("Could not load AssemblyResolver from CommonTypes");
                }
                var startMethod = resolverType.GetMethod("StartLimitedAssemblyResolver");
                if (startMethod is null) {
                    throw new MissingMethodException(
                        "AssemblyResolver does not provide StartForTestFramework method"
                    );
                }
                startMethod.Invoke(null, new object[] { folders });
            } catch(Exception e) {
                Console.WriteLine(
                    "Failed to start assembly resolution issue: " +
                    e.GetType().Name + " " + e.Message
                );
                Console.WriteLine(
                    "Failed to start assembly resolution stack: " +  e.StackTrace
                );
                throw;
            }
        }

        /// <summary>
        /// Find the root archive/working copy folder
        /// </summary>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Retrieving directories can cause many different types of exceptions"
        )]
        private static string FindArchiveRoot(DirectoryInfo directoryInfo) {
            if (directoryInfo == null) {
                return null;
            }
            try {
                string evidence2 = Path.Combine(directoryInfo.FullName, "Build.bat");
                string evidence3 = Path.Combine(directoryInfo.FullName, @"AIP\Output\Bin");

                if (File.Exists(evidence2) && Directory.Exists(evidence3)) {
                    return directoryInfo.FullName;
                }
                return FindArchiveRoot(directoryInfo.Parent);
                #pragma warning disable TI8110 // Do not silently ignore exceptions
            } catch (Exception) {
                #pragma warning restore TI8110 // Do not silently ignore exceptions
                return null;
            }
        }

        /// <summary>
        /// The directory containing the Assembly before shadow copying or installing in the GAC.
        /// </summary>
        private static string OriginalDirectoryLocation(this Assembly assembly) {
            string codeBase = assembly.CodeBase;
            string localPath = new Uri(codeBase).LocalPath;
            return Path.GetDirectoryName(localPath);
        }

        /// <summary>
        /// Attempts to <see cref="Assembly.Load(string)"/> an assembly that could not be resolved.
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            string path = FindAssembly(args.Name, searchPaths);
            return path != null ? Assembly.LoadFrom(path) : null;
        }

        /// <summary>
        /// Checks if the assembly is available on the predefined search paths and return its path.
        /// </summary>
        private static string FindAssembly(string assemblyFullName, IList<string> pathsToSearch ) {
            string[] assemblyDetails = assemblyFullName.Split(',');
            string assemblyName = assemblyDetails[0];
            foreach (string dir in pathsToSearch) {
                string fullPath = Path.Combine(dir, assemblyName + ".dll");
                if (File.Exists(fullPath) && CanLoad(assemblyFullName, fullPath)) {
                    return fullPath;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if the assembly File can be loaded or not.
        /// </summary>
        private static bool CanLoad(string assemblyFullName, string assemblyFile) {
            var requestedAssemblyName = new AssemblyName(assemblyFullName);
            byte[] requestedPublickey = requestedAssemblyName.GetPublicKeyToken();
            string requestedAssemblyPublickey = null;
            if (requestedPublickey != null) {
                requestedAssemblyPublickey = string.Concat(
                    Array.ConvertAll(
                        requestedPublickey,
                        b => b.ToString("x2", CultureInfo.InvariantCulture)
                    )
                );
            }
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyFile);

            string assemblyVersion = assemblyName.Version.ToString();
            string assemblyPublicKey = assemblyName.FullName.Split(',')[3];
            assemblyPublicKey = assemblyPublicKey.Substring(assemblyPublicKey.IndexOf('=') + 1);
            // check version
            bool versionMatches =
                assemblyName.Version.Equals(requestedAssemblyName.Version) ||
                requestedAssemblyName.Version == null;
            bool publicKeyMatches = 
                (
                    !string.IsNullOrEmpty(assemblyPublicKey) && 
                    assemblyPublicKey.Equals(
                        requestedAssemblyPublickey,
                        StringComparison.OrdinalIgnoreCase)
                ) || (
                    requestedAssemblyPublickey == null
                );
            bool canLoad = versionMatches && publicKeyMatches;
            if (!canLoad) {
                string msg = "Cannot Load Assembly " + assemblyFile + ", Reason: ";
                msg +=
                    !versionMatches ?
                    " Invalid version, expected: " + requestedAssemblyName.Version +
                    ", actual:" + assemblyVersion :
                    "";
                msg +=
                    !publicKeyMatches ?
                    " Invalid publicKey , expected: " + requestedAssemblyPublickey +
                    ", actual:" + assemblyPublicKey :
                    "";
                Trace(msg);
            }
            return canLoad;
        }

        private static void Trace(string msg) {
            Console.WriteLine("AssemblyResolverFinder: " + msg);
        }
    }
}
