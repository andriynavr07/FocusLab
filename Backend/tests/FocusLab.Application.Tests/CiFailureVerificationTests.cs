using Xunit;

namespace FocusLab.Application.Tests;

public class CiFailureVerificationTests
{
    [Fact]
    public void IntentionalFailure_ShouldFailCiWorkflow()
    {
        Assert.True(false, "Навмисне падіння тесту для перевірки CI workflow");
    }
}