using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace IconTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string pngPath = @"C:\Users\shiva\Documents\Black-Noir\icon.png";
            string icoPath = @"C:\Users\shiva\Documents\Black-Noir\icon.ico";

            using (Bitmap master = new Bitmap(pngPath))
            {
                int[] sizes = new int[] { 256, 128, 96, 64, 48, 32, 16 };
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter bw = new BinaryWriter(ms);
                    bw.Write((short)0); // Reserved
                    bw.Write((short)1); // Type: 1 = ICO
                    bw.Write((short)sizes.Length); // Image Count

                    int offset = 6 + (16 * sizes.Length);
                    byte[][] pngBytes = new byte[sizes.Length][];

                    for (int i = 0; i < sizes.Length; i++)
                    {
                        int sz = sizes[i];
                        using (Bitmap bmp = new Bitmap(sz, sz))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.DrawImage(master, 0, 0, sz, sz);
                            }
                            using (MemoryStream imgMs = new MemoryStream())
                            {
                                bmp.Save(imgMs, System.Drawing.Imaging.ImageFormat.Png);
                                pngBytes[i] = imgMs.ToArray();
                            }
                        }

                        bw.Write((byte)(sz >= 256 ? 0 : sz));
                        bw.Write((byte)(sz >= 256 ? 0 : sz));
                        bw.Write((byte)0);
                        bw.Write((byte)0);
                        bw.Write((short)1);
                        bw.Write((short)32);
                        bw.Write((int)pngBytes[i].Length);
                        bw.Write((int)offset);

                        offset += pngBytes[i].Length;
                    }

                    for (int i = 0; i < sizes.Length; i++)
                    {
                        bw.Write(pngBytes[i]);
                    }

                    bw.Flush();
                    File.WriteAllBytes(icoPath, ms.ToArray());
                }
            }
            Console.WriteLine("Generated true 256x256 multi-resolution icon.ico successfully!");
        }
    }
}
