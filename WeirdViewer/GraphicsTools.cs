using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WeirdViewer
{
    public class BitmapData
    {
        public int width;
        public int height;
        public byte[] data;

        public void clear()
        {
            for(int i = 0; i<data.Length; ++i) data[i] = 0;
        }

        public void blitTo(BitmapData target, int sourceX, int sourceY, int targetX, int targetY, int width, int height)
        {
            if (sourceX + width > this.width) throw new Exception("Invalid bound: width");
            if (sourceY + height > this.height) throw new Exception("Invalid bound: height");

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int sourceBase = ((sourceY + y) * this.width + (sourceX + x)) * 4;
                    int targetBase = ((targetY + y) * target.width + (targetX + x)) * 4;
                    for (int c = 0; c < 4; ++c) target.data[targetBase + c] = data[sourceBase + c];
                }
            }
        }

        public ImageSource ToImageSource()
        {
            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            System.Drawing.Bitmap bmp = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, pointer);
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);

            pinnedArray.Free();
            ms.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }

    class GraphicsTools
    {
        public static BitmapData LoadKtx(byte[] ktxbz)
        {
            var ms = new MemoryStream(ktxbz);
            var ktx = DecompressStream(ms);

            BCnEncoder.Decoder.BcDecoder decoder = new BCnEncoder.Decoder.BcDecoder();
            var image = decoder.Decode(new MemoryStream(ktx));

            for (int i = 0; i < image.Width * image.Height; ++i)
            {
                int base_addr = 4 * i;
                //BGRA to 
                //RGBA
                var temp = image.data[base_addr];
                image.data[base_addr] = image.data[base_addr + 2];
                image.data[base_addr + 2] = temp;
            }

            BitmapData bd = new BitmapData()
            {
                data = image.data,
                width = image.Width,
                height = image.Height
            };
            return bd;
        }

        private static byte[] KtxToPng(Stream ktxStream)
        {
            BCnEncoder.Decoder.BcDecoder decoder = new BCnEncoder.Decoder.BcDecoder();
            var image = decoder.Decode(ktxStream);

            for (int i = 0; i < image.Width * image.Height; ++i)
            {
                int base_addr = 4 * i;
                //BGRA to 
                //RGBA
                var temp = image.data[base_addr];
                image.data[base_addr] = image.data[base_addr + 2];
                image.data[base_addr + 2] = temp;
            }

            GCHandle pinnedArray = GCHandle.Alloc(image.data, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            System.Drawing.Bitmap bmp = new Bitmap(image.Width, image.Height, image.Width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, pointer);
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);

            pinnedArray.Free();
            return ms.ToArray();
        }

        private static byte[] DecompressStream(MemoryStream ms)
        {
            ms.Position = 2;
            var Decompressed = new MemoryStream();

            using (var d = new DeflateStream(ms, CompressionMode.Decompress))
            {
                d.CopyTo(Decompressed);
                return Decompressed.ToArray();
            }
        }
    }
}
