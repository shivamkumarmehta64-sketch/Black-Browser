using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class MakeIcon
{
    static void Main()
    {
        string inputPath = @"C:\Users\shiva\.gemini\antigravity-cli\brain\9b2d0d52-3373-4761-b4e8-152d25777029\futuristic_black_browser_logo_1785499356692.jpg";
        string outputIco = @"C:\Users\shiva\Documents\Black-Noir\icon.ico";
        string outputPng = @"C:\Users\shiva\Documents\Black-Noir\icon.png";

        if (!File.Exists(inputPath))
        {
            Console.WriteLine("Input image not found: " + inputPath);
            return;
        }

        using (Bitmap src = new Bitmap(inputPath))
        {
            src.Save(outputPng, ImageFormat.Png);

            int[] sizes = new int[] { 256, 128, 96, 64, 48, 32, 16 };

            using (FileStream fs = new FileStream(outputIco, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0); // Reserved
                bw.Write((ushort)1); // Type: ICO
                bw.Write((ushort)sizes.Length); // Count

                int offset = 6 + (16 * sizes.Length);

                byte[][] pngBuffers = new byte[sizes.Length][];

                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    using (Bitmap resized = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.DrawImage(src, 0, 0, sz, sz);
                        }

                        using (MemoryStream ms = new MemoryStream())
                        {
                            resized.Save(ms, ImageFormat.Png);
                            pngBuffers[i] = ms.ToArray();
                        }
                    }

                    int bWidth = sz >= 256 ? 0 : sz;
                    int bHeight = sz >= 256 ? 0 : sz;

                    bw.Write((byte)bWidth);
                    bw.Write((byte)bHeight);
                    bw.Write((byte)0); // Colors
                    bw.Write((byte)0); // Reserved
                    bw.Write((ushort)1); // Planes
                    bw.Write((ushort)32); // BPP
                    bw.Write((uint)pngBuffers[i].Length); // Size
                    bw.Write((uint)offset); // Offset

                    offset += pngBuffers[i].Length;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngBuffers[i]);
                }
            }

            Console.WriteLine("Master Icon ICO successfully generated at: " + outputIco);
        }
    }
}
