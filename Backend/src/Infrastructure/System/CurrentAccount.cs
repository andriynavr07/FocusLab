using FocusLab.Application.Abstractions;

namespace FocusLab.Infrastructure.SystemServices;

public class CurrentAccount : ICurrentAccount
{
    public int? AccountId { get; private set; }
    public bool IsSignedIn => AccountId.HasValue;
    public void SignIn(int accountId) => AccountId = accountId;
    public void SignOut() => AccountId = null;
}