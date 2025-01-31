using System.Runtime.InteropServices;
using System.Text;

public class PasswordVault {
    private const int CRED_TYPE_GENERIC = 1;
    private const int CRED_PERSIST_LOCAL_MACHINE = 2;

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

    public void StoreCredential(string targetName, string username, string password) {
        var passwordBytes = Encoding.Unicode.GetBytes(password);
        var credential = new CREDENTIAL {
            Type = CRED_TYPE_GENERIC,
            TargetName = targetName,
            UserName = username,
            CredentialBlobSize = passwordBytes.Length,
            CredentialBlob = Marshal.AllocHGlobal(passwordBytes.Length),
            Persist = CRED_PERSIST_LOCAL_MACHINE
        };

        try {
            Marshal.Copy(passwordBytes, 0, credential.CredentialBlob, passwordBytes.Length);

            if (!CredWriteW(ref credential, 0)) {
                throw new Exception($"Failed to store credential. Error: {Marshal.GetLastWin32Error()}");
            }
        } finally {
            if (credential.CredentialBlob != IntPtr.Zero) {
                Marshal.FreeHGlobal(credential.CredentialBlob);
            }
        }
    }

    public (string Username, string Password) RetrieveCredential(string targetName) {
        IntPtr credentialPtr = IntPtr.Zero;

        try {
            if (!CredReadW(targetName, CRED_TYPE_GENERIC, 0, out credentialPtr)) {
                throw new Exception($"Failed to retrieve credential. Error: {Marshal.GetLastWin32Error()}");
            }

            var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);

            byte[] passwordBytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, credential.CredentialBlobSize);

            string password = Encoding.Unicode.GetString(passwordBytes);
            return (credential.UserName, password);
        } finally {
            if (credentialPtr != IntPtr.Zero) {
                CredFree(credentialPtr);
            }
        }
    }

    public void DeleteCredential(string targetName) {
        if (!CredDeleteW(targetName, CRED_TYPE_GENERIC, 0)) {
            throw new Exception($"Failed to delete credential. Error: {Marshal.GetLastWin32Error()}");
        }
    }

    public bool CredentialExists(string targetName) {
        IntPtr credentialPtr = IntPtr.Zero;
        try {
            return CredReadW(targetName, CRED_TYPE_GENERIC, 0, out credentialPtr);
        } finally {
            if (credentialPtr != IntPtr.Zero) {
                CredFree(credentialPtr);
            }
        }
    }
}
