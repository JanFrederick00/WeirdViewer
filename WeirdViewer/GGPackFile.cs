using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WeirdViewer
{
    class GGPackFile
    {
        private Stream file;
        private BinaryReader reader;

        private GGDict dict;

        private List<FileEntry> Files = new List<FileEntry>();

        class FileEntry
        {
            public string FileName;
            public uint Offset;
            public uint Size;
        }

        public GGPackFile(Stream file, string path)
        {
            reader = new BinaryReader(file);
            this.file = file;
            file.Position = 0;
            uint offset = reader.ReadUInt32();
            uint length = reader.ReadUInt32();

            file.Position = offset;
            byte[] dictData = reader.ReadBytes((int)length);


            RtMIKeyReader.ComputeXOR(ref dictData, path + "aa");
            var dictStream = new MemoryStream(dictData);
            dict = new GGDict(dictStream, true);

            if (!dict.Root.ContainsKey("files") || dict.Root["files"].GetType() != typeof(object).MakeArrayType()) throw new Exception("Dictionary does not contain files or files is not an array.");
            Dictionary<string, object>[] files = (dict.Root["files"] as object[]).Select(s => s as Dictionary<string, object>).Where(o => o != null).ToArray();

            foreach (var fileinfo in files)
            {
                FileEntry entry = new FileEntry();
                entry.FileName = fileinfo["filename"] as string ?? "";
                entry.Offset = (uint)(int)fileinfo["offset"];
                entry.Size = (uint)(int)fileinfo["size"];

                Files.Add(entry);
            }

            if (dict.Root.ContainsKey("guid")) Console.WriteLine($"Opened archive {dict.Root["guid"]}");
        }

        public byte[] GetFile(string filename)
        {
            var entry = GetFileEntry(filename);
            if (entry == null) throw new FileNotFoundException($"File {filename} not found in pack");

            file.Position = (int)entry.Offset;
            byte[] data = reader.ReadBytes((int)entry.Size);
            string extension = Path.GetExtension(filename).TrimStart('.');
            if (extension.ToLower() == "bank") return data;

            RtMIKeyReader.ComputeXOR(ref data, "");

            if (extension.ToLower() != "yack") return data;
            RtMIKeyReader.ComputeXOR(ref data, "");

            return data;
        }

        public bool FileExists(string filename) => GetFileEntry(filename) != null;

        private FileEntry GetFileEntry(string filename) => Files.Where(f => f.FileName.ToLower() == filename.ToLower()).FirstOrDefault();

        public string[] EnumerateFilesByExtension(string extension)
        {
            extension = extension.ToLower().TrimStart('.');
            return Files.Where(a => Path.GetExtension(a.FileName).ToLower().TrimStart('.') == extension || extension == null).Select(s => s.FileName).ToArray();
        }
    }
}
