using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsCredentialStoreTestClient {

    /// <summary>
    /// Provides safe native methods for interacting with the Windows password vault.
    /// </summary>
    //@AdapterType: Service - This class only uses Microsoft win32 API to store credentials
    internal static class SafeNativeMethods {

        private const int credTypeGeneric = 1;
        private const int credPersistLocalMachine = 2;

        /// <summary>
        /// Stores the specified credential in the Windows password vault.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        /// <param name="username">The username for the credential.</param>
        /// <param name="password">The password for the credential.</param>
        internal static void StoreCredential(string targetName, string username, string password) {
            byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
            var credential = new CREDENTIAL {
                Type = credTypeGeneric,
                TargetName = targetName,
                UserName = username,
                CredentialBlobSize = passwordBytes.Length,
                CredentialBlob = Marshal.AllocHGlobal(passwordBytes.Length),
                Persist = credPersistLocalMachine
            };

            try {
                Marshal.Copy(passwordBytes, 0, credential.CredentialBlob, passwordBytes.Length);
                if (!CredWriteW(ref credential, 0)) {
                    string message = $"Failed to store credential. Error: {Marshal.GetLastWin32Error()}";
                    throw new Win32Exception(message);
                }
            } finally {
                if (credential.CredentialBlob != IntPtr.Zero) {
                    Marshal.FreeHGlobal(credential.CredentialBlob);
                }
            }
        }

        /// <summary>
        /// Retrieves the credential from the Windows password vault for the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        /// <returns>A tuple containing the username and password for the credential.</returns>
        internal static (string Username, string Password) RetrieveCredential(string targetName) {
            IntPtr credentialPtr = IntPtr.Zero;

            try {
                if (!CredReadW(targetName, credTypeGeneric, 0, out credentialPtr)) {
                    string message = $"Failed to retrieve credential. Error: {Marshal.GetLastWin32Error()}";
                    throw new Win32Exception(message);
                }

                var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);

                byte[] passwordBytes = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, credential.CredentialBlobSize);

                string passwordKey = Encoding.Unicode.GetString(passwordBytes);
                return (credential.UserName, passwordKey);
            } finally {
                if (credentialPtr != IntPtr.Zero) {
                    CredFree(credentialPtr);
                }
            }
        }

        /// <summary>
        /// Checks if a credential exists in the Windows password vault for the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        /// <returns>True if the credential exists; otherwise, false.</returns>
        internal static bool CredentialExists(string targetName) {
            IntPtr credentialPtr = IntPtr.Zero;
            try {
                return CredReadW(targetName, credTypeGeneric, 0, out credentialPtr);
            } finally {
                if (credentialPtr != IntPtr.Zero) {
                    CredFree(credentialPtr);
                }
            }
        }

        /// <summary>
        /// Deletes the credential from the Windows password vault for the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        internal static void DeleteCredential(string targetName) {
            if (!CredentialExists(targetName)) {
                return;
            }
            if (!CredDeleteW(targetName, credTypeGeneric, 0)) {
                string message = $"Failed to delete credential. Error: {Marshal.GetLastWin32Error()}";
                throw new Win32Exception(message);
            }
        }

#pragma warning disable PFB4319 // Protect fields.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL {

            public int Flags;
            public int Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }
#pragma warning restore PFB4319 // Protect fields.

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWriteW(
            ref CREDENTIAL credential,
            uint flags);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredReadW(
            string targetName,
            int type,
            int reservedFlag,
            out IntPtr credentialPtr);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredDeleteW(
            string targetName,
            int type,
            int flags);

        [DllImport("advapi32", SetLastError = true)]
        private static extern void CredFree(IntPtr buffer);

    }
}
