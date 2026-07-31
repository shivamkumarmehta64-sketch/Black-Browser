using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace BlackBrowser
{
    public class HistoryItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Timestamp { get; set; }
    }

    public static class HistoryManager
    {
        private static string historyFilePath;
        private static List<HistoryItem> historyList = new List<HistoryItem>();
        private static readonly object fileLock = new object();

        static HistoryManager()
        {
            historyFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "black-webview2", "history.json");
            LoadHistory();
        }

        private static void LoadHistory()
        {
            lock (fileLock)
            {
                try
                {
                    if (File.Exists(historyFilePath))
                    {
                        string json = File.ReadAllText(historyFilePath, Encoding.UTF8);
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        historyList = serializer.Deserialize<List<HistoryItem>>(json) ?? new List<HistoryItem>();
                    }
                }
                catch
                {
                    historyList = new List<HistoryItem>();
                }
            }
        }

        public static void AddVisit(string title, string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url == "about:blank" || url.StartsWith("black://"))
                return;

            lock (fileLock)
            {
                try
                {
                    string pageTitle = string.IsNullOrWhiteSpace(title) ? url : title;
                    string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                    // Avoid duplicate consecutive visits to same URL
                    if (historyList.Count > 0 && historyList[0].Url == url)
                    {
                        historyList[0].Timestamp = timeStr;
                        historyList[0].Title = pageTitle;
                    }
                    else
                    {
                        historyList.Insert(0, new HistoryItem
                        {
                            Title = pageTitle,
                            Url = url,
                            Timestamp = timeStr
                        });
                    }

                    // Keep max 1000 history items locally
                    if (historyList.Count > 1000)
                    {
                        historyList.RemoveRange(1000, historyList.Count - 1000);
                    }

                    SaveHistory();
                }
                catch { }
            }
        }

        public static void ClearHistory()
        {
            lock (fileLock)
            {
                try
                {
                    historyList.Clear();
                    SaveHistory();
                }
                catch { }
            }
        }

        private static void SaveHistory()
        {
            try
            {
                string dir = Path.GetDirectoryName(historyFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(historyList);
                File.WriteAllText(historyFilePath, json, Encoding.UTF8);
            }
            catch { }
        }

        public static string GetHistoryHtml(bool isDarkMode)
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
<title>Local Browsing History — Black Browser</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:" + bg + @";color:" + textColor + @";padding:40px 20px;max-width:800px;margin:0 auto}
.header{display:flex;align-items:center;justify-content:space-between;margin-bottom:30px}
.title{font-size:26px;font-weight:700;color:#1a73e8}
.sub{font-size:13px;color:#80868b;margin-top:4px}
.btn-clear{background:#d93025;color:#fff;border:none;padding:10px 18px;border-radius:18px;font-weight:600;cursor:pointer;font-size:13px}
.btn-clear:hover{background:#a50e0e}
.item{display:flex;align-items:center;justify-content:space-between;padding:14px 20px;background:" + cardBg + @";border:1px solid " + border + @";border-radius:14px;margin-bottom:12px;text-decoration:none;color:inherit;transition:transform .15s ease}
.item:hover{transform:translateY(-2px);border-color:#1a73e8}
.item-title{font-size:15px;font-weight:600;margin-bottom:4px;color:" + textColor + @"}
.item-url{font-size:12.5px;color:#1a73e8;word-break:break-all}
.item-time{font-size:12px;color:#80868b;white-space:nowrap;margin-left:20px}
.empty{text-align:center;padding:60px 0;color:#80868b;font-size:16px}
</style>
</head>
<body>

<div class='header'>
  <div>
    <div class='title'>📜 Local Browsing History</div>
    <div class='sub'>100% Private • Saved on Device (`" + historyFilePath.Replace("\\", "/") + @"`) • No Account Required</div>
  </div>
</div>

<div id='list'>");

            lock (fileLock)
            {
                if (historyList.Count == 0)
                {
                    sb.Append("<div class='empty'>No local browsing history yet. Visit any web page to start logging!</div>");
                }
                else
                {
                    foreach (var item in historyList)
                    {
                        string safeTitle = System.Web.HttpUtility.HtmlEncode(item.Title);
                        string safeUrl = System.Web.HttpUtility.HtmlEncode(item.Url);
                        string safeTime = System.Web.HttpUtility.HtmlEncode(item.Timestamp);

                        sb.Append(@"
<a class='item' href='" + safeUrl + @"'>
  <div>
    <div class='item-title'>" + safeTitle + @"</div>
    <div class='item-url'>" + safeUrl + @"</div>
  </div>
  <div class='item-time'>" + safeTime + @"</div>
</a>");
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
