using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class ThemeViewModelTest
{
    private IThemeController _controller = null!;

    [SetUp]
    public void SetUp() => _controller = A.Fake<IThemeController>();

    [Test]
    public void Constructor_AppliesInitialTheme()
    {
        _ = new ThemeViewModel(_controller, isDark: true);

        A.CallTo(() => _controller.Apply(true)).MustHaveHappened();
    }

    [Test]
    public void Toggle_FlipsAppliesAndRaisesChanged()
    {
        var vm = new ThemeViewModel(_controller, isDark: false);
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.ToggleCommand.Execute(null);

        Assert.That(vm.IsDark, Is.True);
        A.CallTo(() => _controller.Apply(true)).MustHaveHappened();
        Assert.That(raised, Is.True);
    }

    [Test]
    public void IsDark_SetViaProperty_AppliesAndRaisesChanged()
    {
        var vm = new ThemeViewModel(_controller, isDark: false);
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.IsDark = true;

        A.CallTo(() => _controller.Apply(true)).MustHaveHappened();
        Assert.That(raised, Is.True);
    }

    [Test]
    public void SelectedTheme_Light_WhenNotDark()
    {
        var vm = new ThemeViewModel(_controller, isDark: false);

        Assert.That(vm.SelectedTheme, Is.EqualTo(AppTheme.Light));
    }

    [Test]
    public void SelectedTheme_Dark_WhenDark()
    {
        var vm = new ThemeViewModel(_controller, isDark: true);

        Assert.That(vm.SelectedTheme, Is.EqualTo(AppTheme.Dark));
    }

    [Test]
    public void SelectedTheme_SetToDark_TurnsDarkOn()
    {
        var vm = new ThemeViewModel(_controller, isDark: false);

        vm.SelectedTheme = AppTheme.Dark;

        Assert.That(vm.IsDark, Is.True);
        A.CallTo(() => _controller.Apply(true)).MustHaveHappened();
    }

    [Test]
    public void SelectedTheme_SetToLight_TurnsDarkOff()
    {
        var vm = new ThemeViewModel(_controller, isDark: true);

        vm.SelectedTheme = AppTheme.Light;

        Assert.That(vm.IsDark, Is.False);
        A.CallTo(() => _controller.Apply(false)).MustHaveHappened();
    }

    [Test]
    public void IsDarkChange_NotifiesSelectedTheme()
    {
        var vm = new ThemeViewModel(_controller, isDark: false);
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsDark = true;

        Assert.That(changed, Does.Contain(nameof(ThemeViewModel.SelectedTheme)));
    }
}
