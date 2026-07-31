using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlackBrowser
{
    public class DownloadItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string TotalBytes { get; set; }
        public string DateAdded { get; set; }
    }

    public static class DownloadsManager
    {
        private static List<DownloadItem> downloadsList = new List<DownloadItem>();
        private static readonly object fileLock = new object();

        public static void AddDownload(string fileName, string filePath, long totalBytes)
        {
            lock (fileLock)
            {
                double mb = Math.Round(totalBytes / (1024.0 * 1024.0), 2);
                string sizeStr = mb > 0 ? mb.ToString() + " MB" : "Unknown Size";

                downloadsList.Insert(0, new DownloadItem
                {
                    FileName = fileName,
                    FilePath = filePath,
                    TotalBytes = sizeStr,
                    DateAdded = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                });
            }
        }

        public static string GetDownloadsHtml(bool isDarkMode)
        {
            string bg = isDarkMode ? "#121216" : "#f5f5f7";
            string textColor = isDarkMode ? "#ffffff" : "#1d1d21";
            string cardBg = isDarkMode ? "#1c1c24" : "#ffffff";
            string border = isDarkMode ? "rgba(255,255,255,0.08)" : "rgba(0,0,0,0.06)";

            StringBuilder sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<title>Local Downloads — Black Browser</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:" + bg + @";color:" + textColor + @";padding:40px 20px;max-width:800px;margin:0 auto}
.header{display:flex;align-items:center;justify-content:space-between;margin-bottom:30px}
.title{font-size:26px;font-weight:700;color:#107c41}
.sub{font-size:13px;color:#80868b;margin-top:4px}
.item{display:flex;align-items:center;justify-content:space-between;padding:16px 20px;background:" + cardBg + @";border:1px solid " + border + @";border-radius:14px;margin-bottom:12px;color:inherit}
.item-name{font-size:15px;font-weight:600;margin-bottom:4px;color:" + textColor + @"}
.item-path{font-size:12.5px;color:#80868b;word-break:break-all}
.item-size{font-size:12.5px;font-weight:600;color:#107c41;white-space:nowrap;margin-left:20px}
.empty{text-align:center;padding:60px 0;color:#80868b;font-size:16px}
</style>
</head>
<body>

<div class='header'>
  <div>
    <div class='title'>📥 Local Downloads Manager</div>
    <div class='sub'>100% Offline • Zero Account Sign-in Required</div>
  </div>
</div>

<div id='list'>");

            lock (fileLock)
            {
                if (downloadsList.Count == 0)
                {
                    sb.Append("<div class='empty'>No active or recent downloads logged. Downloaded files will appear here automatically!</div>");
                }
                else
                {
                    foreach (var item in downloadsList)
                    {
                        string safeName = System.Web.HttpUtility.HtmlEncode(item.FileName);
                        string safePath = System.Web.HttpUtility.HtmlEncode(item.FilePath);
                        string safeSize = System.Web.HttpUtility.HtmlEncode(item.TotalBytes);

                        sb.Append(@"
<div class='item'>
  <div>
    <div class='item-name'>📄 " + safeName + @"</div>
    <div class='item-path'>" + safePath + @"</div>
  </div>
  <div class='item-size'>" + safeSize + @"</div>
</div>");
                    }
                }
            }

            sb.Append(@"
</div>

</body>
</html>");
            return sb.ToString();
        }
    }
}
