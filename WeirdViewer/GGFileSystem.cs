using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WeirdViewer
{
    public class GGFileSystem
    {
        List<GGPackFile> PackFiles = new List<GGPackFile>();

        public GGFileSystem(string location)
        {
            List<string> FileNames = new List<string>();
            for (int i = 0; i < 10; ++i)
            {
                FileNames.Add($"Weird.ggpack{i}");
                for (char c = 'a'; c < 'h'; c++)
                {
                    FileNames.Add($"Weird.ggpack{i}{c}");
                }
            }

            FileNames = FileNames.Select(f => Path.Combine(location, f)).ToList();

            foreach (var file in FileNames)
            {
                if (File.Exists(file))
                {
                    PackFiles.Add(new GGPackFile(File.OpenRead(file), file));
                }
            }
        }

        public byte[] GetFile(string filename)
        {
            foreach (var pf in PackFiles)
            {
                if (pf.FileExists(filename)) return pf.GetFile(filename);
            }
            throw new FileNotFoundException($"file {filename} not found.");
        }

        public bool FileExists(string filename)
        {
            foreach (var pf in PackFiles)
            {
                if (pf.FileExists(filename)) return true;
            }
            return false;
        }

        public string[] EnumerateFilesByExtension(string extension) => PackFiles.SelectMany(p => p.EnumerateFilesByExtension(extension)).ToArray();
    }
}
