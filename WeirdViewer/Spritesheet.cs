using System;
using System.Collections.Generic;
using System.IO;

namespace WeirdViewer
{
    public class Spritesheet
    {
        private readonly GGDict json;
        private readonly BitmapData fullSheet;

        public class SubSprite
        {
            public string Name;
            public int sheet_w, sheet_h, sheet_x, sheet_y;
            public int width, height;
            public int x, y, w, h;
            public BitmapData ImageData;
            public System.Windows.Media.ImageSource Bitmap;

            public void ExtractFromSheet(BitmapData fullSheet)
            {
                ImageData = new BitmapData() { data = new byte[width * height * 4], height = height, width = width };
                ImageData.clear();

                if (w != sheet_w || h != sheet_h)
                {
                    throw new Exception("Source size != target size?");
                }
                fullSheet.blitTo(ImageData, sheet_x, sheet_y, x, y, w, h);
                Bitmap = ImageData.ToImageSource();
            }
        }

        public Dictionary<string, SubSprite> subSprites = new Dictionary<string, SubSprite>();

        public Spritesheet(byte[] spritesheetFile, GGFileSystem ggfs)
        {
            json = new GGDict(new MemoryStream(spritesheetFile), true);
            string imageFileName = (json.Root["meta"] as Dictionary<string, object>)?["image"] as string ?? "";
            if (imageFileName.EndsWith(".png")) imageFileName = Path.GetFileNameWithoutExtension(imageFileName) + ".ktxbz";
            fullSheet = GraphicsTools.LoadKtx(ggfs.GetFile(imageFileName));

            Dictionary<string, object> frames = json.Root["frames"] as Dictionary<string, object>;
            foreach (var frame in frames)
            {
                var frameData = frame.Value as Dictionary<string, object>;
                var frameRect = frameData["frame"] as Dictionary<string, object>;
                var sourceSize = frameData["sourceSize"] as Dictionary<string, object>;
                var sourceRect = frameData["spriteSourceSize"] as Dictionary<string, object>;

                SubSprite sprite = new SubSprite
                {
                    sheet_w = (int)frameRect["w"],
                    sheet_h = (int)frameRect["h"],
                    sheet_x = (int)frameRect["x"],
                    sheet_y = (int)frameRect["y"],
                    height = (int)sourceSize["h"],
                    width = (int)sourceSize["w"],
                    h = (int)sourceRect["h"],
                    w = (int)sourceRect["w"],
                    x = (int)sourceRect["x"],
                    y = (int)sourceRect["y"],
                    Name = frame.Key
                };
                sprite.ExtractFromSheet(fullSheet);
                subSprites[frame.Key.ToLower()] = sprite;
            }

            fullSheet = null;
            json = null;
        }

        public bool ContainsSprite(string name) => subSprites.ContainsKey(name.ToLower());
        public SubSprite GetSprite(string name) => ContainsSprite(name) ? subSprites[name.ToLower()] : null;
    }
}
