using FluentAssertions;
using FocusLab.Infrastructure.SystemServices;

namespace FocusLab.Infrastructure.Tests;

/// <summary>#73</summary>
public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void HashThenVerify_True()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        _hasher.Verify("correct horse battery staple", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_False()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        _hasher.Verify("wrong password", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePassword_DifferentSalt()
    {
        var first = _hasher.Hash("same password");
        var second = _hasher.Hash("same password");

        first.Should().NotBe(second);
        // формат: v1.<ітерації>.<сіль>.<ключ>
        first.Split('.')[2].Should().NotBe(second.Split('.')[2]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("v1.abc.def.ghi")]        // ітерації не число
    [InlineData("v1.1000.AAAA")]          // замало частин
    [InlineData("v2.1000.AAAA.AAAA")]     // невідома версія
    [InlineData("v1.1000.!!!.!!!")]       // не base64
    public void Verify_MalformedHash_ReturnsFalseNotThrows(string malformed)
    {
        var verify = () => _hasher.Verify("password", malformed);

        verify.Should().NotThrow();
        _hasher.Verify("password", malformed).Should().BeFalse();
    }
}
