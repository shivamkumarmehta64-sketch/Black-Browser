using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace BlackBrowser
{
    public class BookmarkItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string AddedDate { get; set; }
    }

    public static class BookmarksManager
    {
        private static string bookmarksFilePath;
        private static List<BookmarkItem> bookmarksList = new List<BookmarkItem>();
        private static readonly object fileLock = new object();

        static BookmarksManager()
        {
            bookmarksFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "black-webview2", "bookmarks.json");
            LoadBookmarks();
        }

        private static void LoadBookmarks()
        {
            lock (fileLock)
            {
                try
                {
                    if (File.Exists(bookmarksFilePath))
                    {
                        string json = File.ReadAllText(bookmarksFilePath, Encoding.UTF8);
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        bookmarksList = serializer.Deserialize<List<BookmarkItem>>(json) ?? new List<BookmarkItem>();
                    }
                }
                catch
                {
                    bookmarksList = new List<BookmarkItem>();
                }
            }
        }

        public static bool IsBookmarked(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            lock (fileLock)
            {
                return bookmarksList.Exists(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool ToggleBookmark(string title, string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url == "about:blank" || url.StartsWith("black://"))
                return false;

            lock (fileLock)
            {
                int index = bookmarksList.FindIndex(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    bookmarksList.RemoveAt(index);
                    SaveBookmarks();
                    return false; // Removed
                }
                else
                {
                    bookmarksList.Insert(0, new BookmarkItem
                    {
                        Title = string.IsNullOrWhiteSpace(title) ? url : title,
                        Url = url,
                        AddedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                    });
                    SaveBookmarks();
                    return true; // Added
                }
            }
        }

        private static void SaveBookmarks()
        {
            try
            {
                string dir = Path.GetDirectoryName(bookmarksFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(bookmarksList);
                File.WriteAllText(bookmarksFilePath, json, Encoding.UTF8);
            }
            catch { }
        }

        public static string GetBookmarksHtml(bool isDarkMode)
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
<title>Local Bookmarks — Black Browser</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:" + bg + @";color:" + textColor + @";padding:40px 20px;max-width:800px;margin:0 auto}
.header{display:flex;align-items:center;justify-space-between;margin-bottom:30px}
.title{font-size:26px;font-weight:700;color:#f29900}
.sub{font-size:13px;color:#80868b;margin-top:4px}
.item{display:flex;align-items:center;justify-content:space-between;padding:14px 20px;background:" + cardBg + @";border:1px solid " + border + @";border-radius:14px;margin-bottom:12px;text-decoration:none;color:inherit;transition:transform .15s ease}
.item:hover{transform:translateY(-2px);border-color:#f29900}
.item-title{font-size:15px;font-weight:600;margin-bottom:4px;color:" + textColor + @"}
.item-url{font-size:12.5px;color:#1a73e8;word-break:break-all}
.item-time{font-size:12px;color:#80868b;white-space:nowrap;margin-left:20px}
.empty{text-align:center;padding:60px 0;color:#80868b;font-size:16px}
</style>
</head>
<body>

<div class='header'>
  <div>
    <div class='title'>⭐ Local Bookmarks & Saved Sites</div>
    <div class='sub'>100% Offline • Stored locally in `" + bookmarksFilePath.Replace("\\", "/") + @"`</div>
  </div>
</div>

<div id='list'>");

            lock (fileLock)
            {
                if (bookmarksList.Count == 0)
                {
                    sb.Append("<div class='empty'>No saved bookmarks yet. Click the ⭐ star button on any web page to bookmark it!</div>");
                }
                else
                {
                    foreach (var item in bookmarksList)
                    {
                        string safeTitle = System.Web.HttpUtility.HtmlEncode(item.Title);
                        string safeUrl = System.Web.HttpUtility.HtmlEncode(item.Url);
                        string safeTime = System.Web.HttpUtility.HtmlEncode(item.AddedDate);

                        sb.Append(@"
<a class='item' href='" + safeUrl + @"'>
  <div>
    <div class='item-title'>⭐ " + safeTitle + @"</div>
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
