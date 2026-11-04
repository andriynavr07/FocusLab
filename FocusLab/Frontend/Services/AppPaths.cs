using System;
using System.IO;

namespace frontend.Services;

public static class AppPaths
{
    public static string BaseDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusLab");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(BaseDirectory);
    }
}
