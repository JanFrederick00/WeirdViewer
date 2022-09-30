using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WeirdViewer
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: WeirdViewer.exe <Path_to_monkey_island_folder> <filename>.weird");
                return;
            }

            string MonkeyIslandPath = args[0];
            string wimpyName = args[1];

            Console.WriteLine("Opening File System...");
            GGFileSystem ggfs = new GGFileSystem(MonkeyIslandPath);

            if (wimpyName.ToLower() == "--list-all")
            {
                Console.WriteLine("Listing all wimpy files:");
                foreach (var file in ggfs.EnumerateFilesByExtension(".wimpy")) Console.WriteLine(file);
                return;
            }

            Console.WriteLine("Opening wimpy File...");
            byte[] bytes;
            try
            {
                bytes = ggfs.GetFile(wimpyName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not extract file {wimpyName}: {ex.Message}");
                return;
            }

            WimpyScene ws;
            try
            {
                ws = new WimpyScene(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open wimpy: {ex.Message}");
                return;
            }

            Console.WriteLine("Loading Spritesheets...");
            List<Spritesheet> sheets = new List<Spritesheet>();
            try
            {
                if (ggfs.FileExists($"{ws.Sheet}-hd.json"))
                {
                    Console.WriteLine($"Loading spritesheet {ws.Sheet}-hd.json...");
                    sheets.Add(new Spritesheet(ggfs.GetFile($"{ws.Sheet}-hd.json"), ggfs));
                }
                else
                {
                    int i = 0;
                    while (ggfs.FileExists($"{ws.Sheet}{i}-hd.json"))
                    {
                        Console.WriteLine($"Loading spritesheet {ws.Sheet}{i}-hd.json...");
                        sheets.Add(new Spritesheet(ggfs.GetFile($"{ws.Sheet}{i}-hd.json"), ggfs));
                        ++i;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load spritesheet: {ex.Message}");
                return;
            }

            Console.WriteLine($"{sheets.Count} spritesheets loaded.");

            WimpyViewer wv = new WimpyViewer(ws, sheets.ToArray());
            wv.ShowDialog();
        }
    }
}
