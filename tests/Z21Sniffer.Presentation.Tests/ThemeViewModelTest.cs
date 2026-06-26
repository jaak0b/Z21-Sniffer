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
}
