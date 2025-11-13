using System.Runtime.InteropServices;
using System.Text;

namespace Crontab.Services;

public interface ICredentialService
{
    void StorePassword(string username, string password);
    string? GetPassword(string username);
    void RemovePassword();
    bool HasStoredPassword();
}

public class CredentialService : ICredentialService
{
    private const string TargetName = "Crontab_TaskScheduler";

    public void StorePassword(string username, string password)
    {
        var credential = new CREDENTIAL
        {
            Type = CRED_TYPE.GENERIC,
            TargetName = TargetName,
            UserName = username,
            CredentialBlob = Marshal.StringToCoTaskMemUni(password),
            CredentialBlobSize = (uint)(password.Length * 2), // Unicode = 2 bytes per char
            Persist = CRED_PERSIST.LOCAL_MACHINE,
            AttributeCount = 0,
            Attributes = IntPtr.Zero,
            Comment = null,
            TargetAlias = null
        };

        try
        {
            if (!CredWrite(ref credential, 0))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to write credential. Error code: {error}");
            }
        }
        finally
        {
            if (credential.CredentialBlob != IntPtr.Zero)
            {
                Marshal.ZeroFreeCoTaskMemUnicode(credential.CredentialBlob);
            }
        }
    }

    public string? GetPassword(string username)
    {
        IntPtr credPtr = IntPtr.Zero;
        try
        {
            if (CredRead(TargetName, CRED_TYPE.GENERIC, 0, out credPtr))
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                if (credential.CredentialBlobSize > 0)
                {
                    return Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
                }
            }
            return null;
        }
        finally
        {
            if (credPtr != IntPtr.Zero)
            {
                CredFree(credPtr);
            }
        }
    }

    public void RemovePassword()
    {
        CredDelete(TargetName, CRED_TYPE.GENERIC, 0);
    }

    public bool HasStoredPassword()
    {
        IntPtr credPtr = IntPtr.Zero;
        try
        {
            return CredRead(TargetName, CRED_TYPE.GENERIC, 0, out credPtr);
        }
        finally
        {
            if (credPtr != IntPtr.Zero)
            {
                CredFree(credPtr);
            }
        }
    }

    #region P/Invoke Declarations

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(
        string target,
        CRED_TYPE type,
        int reservedFlag,
        out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(
        [In] ref CREDENTIAL userCredential,
        uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(
        string target,
        CRED_TYPE type,
        int reservedFlag);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? UserName;
    }

    private enum CRED_TYPE : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7,
        MAXIMUM_EX = 1007
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }

    #endregion
}
