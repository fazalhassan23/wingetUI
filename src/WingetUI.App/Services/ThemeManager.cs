using System.Windows;

namespace WingetUI.App.Services;

public sealed class ThemeManager(ResourceDictionary resources)
{
    private readonly Dictionary<object, object> darkResources = [];
    private bool usingSystemTheme;

    public void ApplySystemTheme()
    {
        if (usingSystemTheme)
            return;

        foreach (var key in resources.Keys.Cast<object>().Where(key => key is ResourceKey).ToArray())
        {
            darkResources[key] = resources[key];
            resources.Remove(key);
        }
        usingSystemTheme = true;
    }

    public void Toggle()
    {
        if (SystemParameters.HighContrast)
            return;

        if (!usingSystemTheme)
        {
            ApplySystemTheme();
            return;
        }

        foreach (var resource in darkResources)
            resources[resource.Key] = resource.Value;
        usingSystemTheme = false;
    }
}
