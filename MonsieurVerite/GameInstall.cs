using System.IO;
using Microsoft.Win32;

namespace MonsieurVerite;

public static class GameInstall
{
    public static string? CutsceneFolder()
    {
        if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Cognosphere\HYP\1_0\hk4e_global",
                "GameInstallPath", null) is not string { Length: > 0 } game)
        {
            return null;
        }

        var folder = Path.GetFullPath(Path.Combine(game, "GenshinImpact_Data", "StreamingAssets",
            "VideoAssets", "StandaloneWindows64"));
        return Directory.Exists(folder) ? folder : null;
    }
}
