// Copyright Koninklijke Philips N.V. 2018

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

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
    /// 1) AssemblyResolverFinder.AddSearchPath(PathToPlatform);
    ///    [not needed inside regular platform developer/subsystem deployments]
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

            // We load via reflection to avoid unwanted code dependencies in a few very specific scenarios.
            // To ensure we have just one code path to test we always use reflection

            // CommonUtilities AsemblyResolver depends on PlatformDeployments from Common.Deployment
            var platformDeploymentName = GetAssemblyName("Philips.Platform.Common.Deployment");
            var platformDeployment = Assembly.Load(platformDeploymentName);

            Type result = null;
            if (platformDeployment != null) {
                PlatformDeploymentLocation = platformDeployment.Location;
                var commonUtilitiesAssemblyName = GetAssemblyName("Philips.Platform.CommonUtilities");
                var commonUtilities = Assembly.Load(commonUtilitiesAssemblyName);
                if (commonUtilities != null) {
                    CommonUtilitiesLocation = commonUtilities.Location;
                    result =
                        commonUtilities.GetType("Philips.Platform.CommonUtilities.AssemblyResolution.AssemblyResolver");
                }
            }
            return result;
        }

        private static AssemblyName GetAssemblyName(string name) {
            const string assemblyVersion = @"25.2.0.0";
            const string publicKeyToken = "223d991ebf2e6ef5";
            var fullName = $"{name}, Version={assemblyVersion}, Culture=neutral, PublicKeyToken={publicKeyToken}";
            return new AssemblyName(fullName);
        }

        internal static string CommonUtilitiesLocation { get; private set; }
        internal static string PlatformDeploymentLocation { get; private set; }

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
            // Assume IF not in Output\Bin external caller has arranged
            // either search paths or alternate assembly resolver.
            bool developerEnvironment = myLocation.IndexOf(@"\Output\Bin", StringComparison.OrdinalIgnoreCase) >= 0;
            if (developerEnvironment) {
                string archiveRoot = FindArchiveRoot(new DirectoryInfo(myLocation));
                if (!string.IsNullOrEmpty(archiveRoot)) {
                    // this is the developer-build environment
                    // we should only need Common and Common\Libraries to enable assembly resolution
                    AddSearchPath(Path.Combine(archiveRoot, @"Common\Output\Bin\"));
                    AddSearchPath(Path.Combine(archiveRoot, @"Common\Libraries\Output\Bin\"));
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
            } catch (Exception e) {
                Trace(
                    "Failed to start assembly resolution issue: " +
                    e.GetType().Name + " " + e.Message
                );
                Trace(
                    "Failed to start assembly resolution stack: " + e.StackTrace
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
            } catch (Exception e) {
                Trace(
                    "Failed to start assembly resolution issue: " +
                    e.GetType().Name + " " + e.Message
                );
                Trace(
                    "Failed to start assembly resolution stack: " + e.StackTrace
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
            // TODO: Use PlatformDeployment directly
            if (directoryInfo == null) {
                return null;
            }
            try {
                string evidence1 = Path.Combine(directoryInfo.FullName, "DeveloperWorkingArea.link");
                string evidence2 = Path.Combine(directoryInfo.FullName, "BuildNX.bat");
                string evidence3 = Path.Combine(directoryInfo.FullName, @"AIP\Output\Bin");

                if (File.Exists(evidence1) && File.Exists(evidence2) && Directory.Exists(evidence3)) {
                    return directoryInfo.FullName;
                }
                return FindArchiveRoot(directoryInfo.Parent);
#pragma warning disable TI8110 // Do not silently ignore exceptions
#pragma warning disable PFB4327 // Do not silently ignore exceptions
            } catch (Exception) {
#pragma warning restore TI8110 // Do not silently ignore exceptions
                return null;
            }
#pragma warning restore PFB4327 // Do not silently ignore exceptions
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
        private static string FindAssembly(string assemblyFullName, IList<string> pathsToSearch) {
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

        private static string TokenToString(byte[] token) {
            if (token is null || token.Length == 0) {
                return string.Empty;
            }
            return string.Concat(Array.ConvertAll(token, b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Checks if the assembly File can be loaded or not.
        /// </summary>
        private static bool CanLoad(string assemblyFullName, string assemblyFullPath) {
            var requestedAssemblyName = new AssemblyName(assemblyFullName);
            byte[] requestedPublickey = requestedAssemblyName.GetPublicKeyToken();
            string requestedAssemblyPublicKey = null;
            if (requestedPublickey != null) {
                requestedAssemblyPublicKey = TokenToString(requestedPublickey);
            }
            AssemblyName foundAssemblyName = AssemblyName.GetAssemblyName(assemblyFullPath);

            string foundAssemblyVersion = foundAssemblyName.Version.ToString();
            var foundAssemblyPublicKeyToken = foundAssemblyName.GetPublicKeyToken();
            string foundAssemblyPublicKey = null;
            if (foundAssemblyPublicKeyToken != null) {
                foundAssemblyPublicKey = TokenToString(foundAssemblyPublicKeyToken);
            }
            // check version
            bool versionMatches =
                foundAssemblyName.Version.Equals(requestedAssemblyName.Version) ||
                requestedAssemblyName.Version == null;
            bool publicKeyMatches =
                StringComparer.OrdinalIgnoreCase.Equals(foundAssemblyPublicKey, requestedAssemblyPublicKey);
            bool canLoad = versionMatches && publicKeyMatches;
            if (!canLoad) {
                string msg = "Cannot Load Assembly " + assemblyFullPath + ", Reason: ";
                msg +=
                    !versionMatches ?
                    " Invalid version, expected: " + requestedAssemblyName.Version +
                    ", actual:" + foundAssemblyVersion :
                    "";
                msg +=
                    !publicKeyMatches ?
                    " Invalid publicKey , expected: " + requestedAssemblyPublicKey +
                    ", actual:" + foundAssemblyPublicKey :
                    "";
                Trace(msg);
            }
            return canLoad;
        }

        private static void Trace(string msg) {
            string message = "AssemblyResolverFinder: " + msg;
            System.Diagnostics.Trace.WriteLine(message);
            // TODO: replace Console.WriteLine by agreed diagnostics
            Console.WriteLine("AssemblyResolverFinder: " + message);
        }
    }
}
