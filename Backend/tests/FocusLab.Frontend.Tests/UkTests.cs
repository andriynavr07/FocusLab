using FluentAssertions;
using Frontend.Services;
using Xunit;

namespace FocusLab.Frontend.Tests;

/// <summary>
/// #108: Uk більше не статичний і бере мову з переданого ILocalizer.
/// Українська й англійська перевіряються в одному тесті — без спільного стану і без [Collection].
/// </summary>
public class UkTests
{
    private readonly Uk _ua = new(TestLocalizers.Ukrainian);
    private readonly Uk _en = new(TestLocalizers.English);

    [Theory]
    [InlineData(1, "завдання")]
    [InlineData(3, "завдання")]
    [InlineData(5, "завдань")]
    [InlineData(11, "завдань")]
    [InlineData(21, "завдання")]
    public void Plural_Ukrainian_ChoosesForm(int value, string expected)
        => _ua.Plural(value, "завдання", "завдання", "завдань").Should().Be(expected);

    [Theory]
    [InlineData(1, "task")]
    [InlineData(5, "tasks")]
    public void Plural_English_TranslatesOneOrMany(int value, string expected)
        => _en.Plural(value, "завдання", "завдання", "завдань").Should().Be(expected);

    [Theory]
    [InlineData(25, "25 хв", "25 min")]
    [InlineData(60, "1 година", "1 hour")]
    [InlineData(120, "2 години", "2 hours")]
    [InlineData(85, "1 год 25 хв", "1 hr 25 min")]
    public void Duration_DependsOnlyOnItsLocalizer(int minutes, string ukrainian, string english)
    {
        _ua.Duration(minutes).Should().Be(ukrainian);
        _en.Duration(minutes).Should().Be(english);
    }

    [Fact]
    public void ShortUpper_FormatsPerLanguage()
    {
        var date = new DateTime(2026, 9, 22);

        _ua.ShortUpper(date, withYear: true).Should().Be("22 ВЕР 2026");
        _en.ShortUpper(date, withYear: true).Should().Be("SEP 22 2026");
    }

    [Fact]
    public void FixedLocalizer_UnsupportedLanguage_Throws()
    {
        var create = () => new FixedLocalizer("Deutsch");

        create.Should().Throw<ArgumentOutOfRangeException>();
    }
}
