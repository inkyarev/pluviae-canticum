using System.IO;
using System.Linq;
using PluviaeCanticum.Songs;

namespace PluviaeCanticum;

public static class Utils
{
    public static FileInfo[] SearchForAudioFiles(string path)
    {
        var info = new DirectoryInfo(path);
        if (!info.Exists) return [];
        
        var files = info.GetFiles();
        return files.Where(file => file.Extension is ".mp3" or ".wav").ToArray();
    }
    
    public static bool ExistsWithin(this Song song, FileInfo[] audioFiles)
    {
        foreach (var file in audioFiles)
        {
            if (song.Name == file.Name)
            {
                song.FilePath = file.FullName;
                return true;
            }
        }

        return false;
    }
}