using Avalonia.Controls;
using Avalonia.Layout;

namespace Z21Sniffer.UI.Desktop.Views;

public sealed class ConfirmDialog : Window
{
    public ConfirmDialog(string message, string yes, string no)
    {
        Width = 380;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        ShowInTaskbar = false;

        var yesButton = new Button { Content = yes, IsDefault = true, MinWidth = 80 };
        yesButton.Click += (_, _) => Close(true);

        var noButton = new Button { Content = no, IsCancel = true, MinWidth = 80 };
        noButton.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { noButton, yesButton }
                }
            }
        };
    }
}
