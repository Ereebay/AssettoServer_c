using Photino.NET;

namespace ACDyno.Services;

/// <summary>
/// Service for accessing native window functionality like folder dialogs
/// </summary>
public class WindowService
{
    private PhotinoWindow? _window;
    
    public void SetWindow(PhotinoWindow window)
    {
        _window = window;
    }
    
    /// <summary>
    /// Open a native folder picker dialog
    /// </summary>
    public string? PickFolder(string title = "Select Folder")
    {
        if (_window == null) return null;
        
        try
        {
            var result = _window.ShowOpenFolder(title);
            return result?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Open a native file picker dialog
    /// </summary>
    public string? PickFile(string title = "Select File")
    {
        if (_window == null) return null;
        
        try
        {
            var result = _window.ShowOpenFile(title);
            return result?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Show a message dialog
    /// </summary>
    public void ShowMessage(string title, string message)
    {
        _window?.ShowMessage(title, message);
    }
}
