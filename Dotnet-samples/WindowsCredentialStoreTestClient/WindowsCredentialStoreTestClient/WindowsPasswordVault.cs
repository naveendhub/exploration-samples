using System;
using System.ComponentModel;

namespace WindowsCredentialStoreTestClient {

    /// <summary>
    /// Represents a Windows password vault for storing and retrieving credentials.
    /// </summary>
    internal class WindowsPasswordVault {
        
        /// <summary>
        /// Stores the specified credential in the Windows password vault.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        /// <param name="username">The username for the credential.</param>
        /// <param name="password">The password for the credential.</param>
        public void StoreCredential(string targetName, string username, string password) {
            try {
                SafeNativeMethods.StoreCredential(targetName, username, password);
            } catch (Exception ex) {
                string message = $"Failed to store credential for target '{ex.Message}'.";
                throw new Win32Exception(message, ex);
            }
        }

        /// <summary>
        /// Retrieves the credential from the Windows password vault for the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        /// <returns>A tuple containing the username and password for the credential.</returns>
        public (string Username, string Password) RetrieveCredential(string targetName) {
            try {
                return SafeNativeMethods.RetrieveCredential(targetName);
            } catch (Exception ex) {
                string message = $"Failed to retrieve credential for target '{ex.Message}'.";
                throw new Win32Exception(message, ex);
            }

        }

        /// <summary>
        /// Checks if a credential exists in the Windows password vault for the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        /// <returns>True if the credential exists; otherwise, false.</returns>
        public bool CredentialExists(string targetName) {
            try {
                return SafeNativeMethods.CredentialExists(targetName);
            } catch (Exception ex) {
                string message = $"Failed to check if credential exists for target '{ex.Message}'.";
                throw new Win32Exception(message, ex);
            }
        }

        /// <summary>
        /// Deletes the credential from the Windows password vault for the specified target.
        /// </summary>
        /// <param name="targetName">The name of the target for the credential.</param>
        public void DeleteCredential(string targetName) {
            try {
                SafeNativeMethods.DeleteCredential(targetName);
            } catch (Exception ex) {
                string message = $"Failed to delete credential for target '{ex.Message}'.";
                throw new Win32Exception(message, ex);
            }

        }
    }
}
