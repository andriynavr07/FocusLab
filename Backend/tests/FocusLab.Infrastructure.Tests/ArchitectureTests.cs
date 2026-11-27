using System.Reflection;
using FluentAssertions;

namespace FocusLab.Infrastructure.Tests;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(FocusLab.Domain.Entities.TaskItem).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(FocusLab.Application.Services.TaskService).Assembly;

    [Fact]
    public void Domain_DoesNotReferenceApplicationOrInfrastructure()
    {
        var referenced = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        referenced.Should().NotContain("FocusLab.Application");
        referenced.Should().NotContain("FocusLab.Infrastructure");
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        var referenced = ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        referenced.Should().NotContain("FocusLab.Infrastructure");
    }
}
