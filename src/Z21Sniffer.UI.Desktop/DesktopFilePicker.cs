using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Z21Sniffer.UI.Desktop;

public sealed class DesktopFilePicker
{
    private readonly TopLevel _topLevel;

    public DesktopFilePicker(TopLevel topLevel) => _topLevel = topLevel;

    public Task<string?> SaveJsonAsync() => PickAsync("session.json", "json", "JSON");

    public async Task<string?> OpenJsonAsync()
    {
        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private async Task<string?> PickAsync(string suggestedName, string extension, string typeName)
    {
        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(typeName) { Patterns = [$"*.{extension}"] }]
        });

        return file?.Path.LocalPath;
    }
}
