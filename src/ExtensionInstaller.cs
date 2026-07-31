using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace BlackBrowser
{
    public static class ExtensionInstaller
    {
        public static string UnpackCrx(string crxFilePath)
        {
            try
            {
                if (!File.Exists(crxFilePath)) return null;

                byte[] data = File.ReadAllBytes(crxFilePath);
                
                // Locate Zip header magic bytes 'PK\x03\x04' (0x50, 0x4B, 0x03, 0x04)
                int zipOffset = -1;
                for (int i = 0; i < data.Length - 4; i++)
                {
                    if (data[i] == 0x50 && data[i + 1] == 0x4B && data[i + 2] == 0x03 && data[i + 3] == 0x04)
                    {
                        zipOffset = i;
                        break;
                    }
                }

                string extName = Path.GetFileNameWithoutExtension(crxFilePath);
                // Sanitize extension folder name
                extName = Regex.Replace(extName, @"[^a-zA-Z0-9_\-]", "_");

                string extensionsBaseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "black-webview2", "Extensions");

                if (!Directory.Exists(extensionsBaseDir))
                    Directory.CreateDirectory(extensionsBaseDir);

                string targetDir = Path.Combine(extensionsBaseDir, extName);

                if (zipOffset != -1)
                {
                    if (Directory.Exists(targetDir))
                        Directory.Delete(targetDir, true);
                    Directory.CreateDirectory(targetDir);

                    using (MemoryStream ms = new MemoryStream(data, zipOffset, data.Length - zipOffset))
                    using (ZipArchive zip = new ZipArchive(ms))
                    {
                        zip.ExtractToDirectory(targetDir);
                    }
                    return targetDir;
                }
                else
                {
                    // Fallback if file is already a raw zip
                    if (Directory.Exists(targetDir))
                        Directory.Delete(targetDir, true);
                    Directory.CreateDirectory(targetDir);

                    ZipFile.ExtractToDirectory(crxFilePath, targetDir);
                    return targetDir;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
