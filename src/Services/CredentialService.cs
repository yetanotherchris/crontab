using CredentialManagement;

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
        using var cred = new Credential
        {
            Target = TargetName,
            Username = username,
            Password = password,
            Type = CredentialType.Generic,
            PersistanceType = PersistanceType.LocalComputer
        };
        cred.Save();
    }

    public string? GetPassword(string username)
    {
        using var cred = new Credential
        {
            Target = TargetName
        };

        if (cred.Load())
        {
            return cred.Password;
        }

        return null;
    }

    public void RemovePassword()
    {
        using var cred = new Credential
        {
            Target = TargetName
        };
        cred.Delete();
    }

    public bool HasStoredPassword()
    {
        using var cred = new Credential
        {
            Target = TargetName
        };
        return cred.Exists();
    }
}
