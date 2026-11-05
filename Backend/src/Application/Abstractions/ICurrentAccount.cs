namespace FocusLab.Application.Abstractions;

public interface ICurrentAccount
{
    int? AccountId { get; }
    bool IsSignedIn { get; }
    void SignIn(int accountId);
    void SignOut();
}