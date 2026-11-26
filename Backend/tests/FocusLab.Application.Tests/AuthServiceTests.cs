using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Services;
using FocusLab.Domain.Entities;
using NSubstitute;
using Xunit;

namespace FocusLab.Application.Tests;

public class AuthServiceTests
{
    private const string ValidPassword = "password123";

    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentAccount _current = Substitute.For<ICurrentAccount>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("HASHED");
        _sut = new AuthService(_accounts, _hasher, _current);
    }

    private static Account FreshLocal() => new()
    {
        Id = Account.LocalAccountId,
        Username = "local",
        Email = "",
        PasswordHash = ""
    };

    private Account RegisteredAccount(string email = "user@example.com")
    {
        var acc = new Account { Id = 1, Username = "User", Email = email, PasswordHash = "HASHED" };
        _accounts.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(acc);
        _accounts.GetByIdAsync(acc.Id, Arg.Any<CancellationToken>()).Returns(acc);
        return acc;
    }

    [Fact]
    public async Task Register_Valid_HashesPasswordAndSignsIn()
    {
        var acc = FreshLocal();
        _accounts.GetLocalAsync(Arg.Any<CancellationToken>()).Returns(acc);

        var result = await _sut.RegisterAsync("Oleh", "oleh@example.com", ValidPassword);

        result.Success.Should().BeTrue();
        acc.Username.Should().Be("Oleh");
        acc.PasswordHash.Should().Be("HASHED").And.NotBe(ValidPassword);
        _hasher.Received(1).Hash(ValidPassword);
        await _accounts.Received(1).UpdateAsync(acc, Arg.Any<CancellationToken>());
        _current.Received(1).SignIn(acc.Id);
    }

    public static IEnumerable<object[]> InvalidNames => new[]
    {
        new object[] { "" },
        new object[] { "   " },
        new object[] { new string('a', 101) }
    };

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public async Task Register_InvalidName_Fails(string name)
    {
        _accounts.GetLocalAsync(Arg.Any<CancellationToken>()).Returns(FreshLocal());

        var result = await _sut.RegisterAsync(name, "a@b.com", ValidPassword);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        _current.DidNotReceive().SignIn(Arg.Any<int>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@")]
    [InlineData("@b.com")]
    [InlineData("a b@c.com")]
    public async Task Register_InvalidEmail_Fails(string email)
    {
        _accounts.GetLocalAsync(Arg.Any<CancellationToken>()).Returns(FreshLocal());

        var result = await _sut.RegisterAsync("Oleh", email, ValidPassword);

        result.Success.Should().BeFalse();
        _current.DidNotReceive().SignIn(Arg.Any<int>());
    }

    [Fact]
    public async Task Register_PasswordUnder8_Fails()
    {
        _accounts.GetLocalAsync(Arg.Any<CancellationToken>()).Returns(FreshLocal());

        var result = await _sut.RegisterAsync("Oleh", "a@b.com", "1234567");

        result.Success.Should().BeFalse();
        _hasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    [Fact]
    public async Task Register_AlreadyRegistered_Fails()
    {
        var acc = FreshLocal();
        acc.PasswordHash = "EXISTING";
        _accounts.GetLocalAsync(Arg.Any<CancellationToken>()).Returns(acc);

        var result = await _sut.RegisterAsync("Oleh", "a@b.com", ValidPassword);

        result.Success.Should().BeFalse();
        await _accounts.DidNotReceive().UpdateAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        _current.DidNotReceive().SignIn(Arg.Any<int>());
    }

    [Fact]
    public async Task Register_NormalizesEmail()
    {
        var acc = FreshLocal();
        _accounts.GetLocalAsync(Arg.Any<CancellationToken>()).Returns(acc);

        var result = await _sut.RegisterAsync("Oleh", "  Oleh@Example.COM  ", ValidPassword);

        result.Success.Should().BeTrue();
        acc.Email.Should().Be("oleh@example.com");
    }

    [Fact]
    public async Task Login_Success_SignsIn()
    {
        var acc = RegisteredAccount();
        _hasher.Verify(ValidPassword, acc.PasswordHash).Returns(true);

        var result = await _sut.LoginAsync("USER@example.com", ValidPassword);

        result.Success.Should().BeTrue();
        _current.Received(1).SignIn(acc.Id);
    }

    [Fact]
    public async Task Login_WrongPassword_Fails()
    {
        var acc = RegisteredAccount();
        _hasher.Verify(Arg.Any<string>(), acc.PasswordHash).Returns(false);

        var result = await _sut.LoginAsync("user@example.com", "wrong-pass");

        result.Success.Should().BeFalse();
        _current.DidNotReceive().SignIn(Arg.Any<int>());
    }

    [Fact]
    public async Task Login_UnknownEmail_SameMessageAsWrongPassword()
    {
        var acc = RegisteredAccount();
        _hasher.Verify(Arg.Any<string>(), acc.PasswordHash).Returns(false);
        _accounts.GetByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var wrongPassword = await _sut.LoginAsync("user@example.com", "wrong-pass");
        var unknownEmail = await _sut.LoginAsync("nobody@example.com", "whatever1");

        unknownEmail.Success.Should().BeFalse();
        unknownEmail.Error.Should().Be(wrongPassword.Error);
    }

    [Fact]
    public async Task UpdateProfile_EmailTakenByOther_Fails()
    {
        var me = RegisteredAccount("me@example.com");
        _current.AccountId.Returns((int?)me.Id);
        _accounts.GetByEmailAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(new Account { Id = 99, Email = "taken@example.com" });

        var result = await _sut.UpdateProfileAsync("New Name", "taken@example.com");

        result.Success.Should().BeFalse();
        await _accounts.DidNotReceive().UpdateAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_NotSignedIn_Fails()
    {
        _current.AccountId.Returns((int?)null);

        var result = await _sut.UpdateProfileAsync("Name", "a@b.com");

        result.Success.Should().BeFalse();
        await _accounts.DidNotReceive().UpdateAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsRegistered_NoPasswordHash_False()
    {
        _accounts.GetLocalAsync(Arg.Any<CancellationToken>()).Returns(FreshLocal());

        (await _sut.IsRegisteredAsync()).Should().BeFalse();
    }
}
