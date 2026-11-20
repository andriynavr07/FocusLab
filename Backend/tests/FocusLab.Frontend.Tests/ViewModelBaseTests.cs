using FluentAssertions;
using Frontend.Services;
using Frontend.ViewModels;
using Xunit;

namespace FocusLab.Frontend.Tests;

public class ViewModelBaseTests
{
    /// <summary>SetField захищений, тому перевіряємо його через мінімальну модель.</summary>
    private sealed class SampleViewModel : BoardBase
    {
        private string _title = "";
        private int _count;

        public string Title { get => _title; set => SetField(ref _title, value); }
        public int Count { get => _count; set => SetField(ref _count, value); }

        public bool SetTitleDirectly(string value) => SetField(ref _title, value, "Custom");

        public void Raise(string? name = null) => OnPropertyChanged(name);
    }

    private static List<string?> Track(SampleViewModel model)
    {
        var raised = new List<string?>();
        model.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        return raised;
    }

    [Fact]
    public void SetField_SameValue_NoPropertyChanged()
    {
        var model = new SampleViewModel { Title = "A" };
        var raised = Track(model);

        model.Title = "A";

        raised.Should().BeEmpty();
    }

    [Fact]
    public void SetField_SameValue_ReturnsFalse()
    {
        var model = new SampleViewModel { Title = "A" };

        model.SetTitleDirectly("A").Should().BeFalse();
    }

    [Fact]
    public void SetField_NewValue_RaisesOnce()
    {
        var model = new SampleViewModel();
        var raised = Track(model);

        model.Title = "B";

        raised.Should().Equal(nameof(SampleViewModel.Title));
    }

    [Fact]
    public void SetField_NewValue_StoresValueAndReturnsTrue()
    {
        var model = new SampleViewModel();

        model.SetTitleDirectly("B").Should().BeTrue();

        model.Title.Should().Be("B");
    }

    [Fact]
    public void SetField_UsesCallerMemberNameForEachProperty()
    {
        var model = new SampleViewModel();
        var raised = Track(model);

        model.Count = 3;
        model.Title = "C";

        raised.Should().Equal(nameof(SampleViewModel.Count), nameof(SampleViewModel.Title));
    }

    [Fact]
    public void SetField_ExplicitPropertyName_IsUsed()
    {
        var model = new SampleViewModel();
        var raised = Track(model);

        model.SetTitleDirectly("D");

        raised.Should().Equal("Custom");
    }

    [Fact]
    public void OnPropertyChanged_WithoutHandlers_DoesNotThrow()
    {
        var model = new SampleViewModel();

        var raise = () => model.Raise("Anything");

        raise.Should().NotThrow();
    }
}
