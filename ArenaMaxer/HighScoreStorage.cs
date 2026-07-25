using System;
using System.Globalization;
using System.IO;

namespace ArenaMaxer;

/// <summary>Loads and saves a high score while safely handling invalid or unavailable files.</summary>
public static class HighScoreStorage
{
    public static int Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return 0;
            string text = File.ReadAllText(path);
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int score)
                ? Math.Max(0, score)
                : 0;
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    public static bool Save(string path, int score)
    {
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, Math.Max(0, score).ToString(CultureInfo.InvariantCulture));
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
