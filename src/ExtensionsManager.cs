using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BlackBrowser
{
    public static class ExtensionsManager
    {
        private static string SimpleHtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '<') sb.Append("&lt;");
                else if (c == '>') sb.Append("&gt;");
                else if (c == '&') sb.Append("&amp;");
                else if (c == '"') sb.Append("&quot;");
                else if (c == '\'') sb.Append("&#39;");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static string GetExtensionsHtml(bool isDarkMode)
        {
            string bg = isDarkMode ? "#181816" : "#f3f5fa";
            string cardBg = isDarkMode ? "rgba(255, 255, 255, 0.05)" : "#ffffff";
            string cardBorder = isDarkMode ? "rgba(255, 255, 255, 0.1)" : "rgba(0, 0, 0, 0.08)";
            string textColor = isDarkMode ? "#ffffff" : "#1d1d21";
            string subTextColor = isDarkMode ? "#9a9ab0" : "#6e6e82";

            StringBuilder listSb = new StringBuilder();

            try
            {
                string extBaseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "black-webview2", "Extensions");

                if (Directory.Exists(extBaseDir))
                {
                    string[] dirs = Directory.GetDirectories(extBaseDir);
                    foreach (string dir in dirs)
                    {
                        string folderName = Path.GetFileName(dir);
                        string manifestPath = Path.Combine(dir, "manifest.json");

                        string name = folderName;
                        string version = "1.0";
                        string description = "Unpacked Browser Extension";

                        if (File.Exists(manifestPath))
                        {
                            string json = File.ReadAllText(manifestPath);
                            var nameMatch = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
                            if (nameMatch.Success) name = nameMatch.Groups[1].Value;

                            var verMatch = Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
                            if (verMatch.Success) version = verMatch.Groups[1].Value;

                            var descMatch = Regex.Match(json, @"""description""\s*:\s*""([^""]+)""");
                            if (descMatch.Success) description = descMatch.Groups[1].Value;
                        }

                        listSb.Append(@"
  <div class='ext-card'>
    <div class='ext-icon'>🧩</div>
    <div class='ext-info'>
      <div class='ext-title'>" + SimpleHtmlEncode(name) + @" <span class='ext-ver'>v" + SimpleHtmlEncode(version) + @"</span></div>
      <div class='ext-desc'>" + SimpleHtmlEncode(description) + @"</div>
      <div class='ext-path'>" + SimpleHtmlEncode(dir) + @"</div>
    </div>
    <div class='ext-status'>
      <span class='status-pill'>Active</span>
    </div>
  </div>");
                    }
                }
            }
            catch { }

            if (listSb.Length == 0)
            {
                listSb.Append(@"
  <div class='empty-state'>
    <div class='empty-icon'>🧩</div>
    <div class='empty-title'>No Extensions Installed Yet</div>
    <div class='empty-desc'>Visit the Chrome Web Store or Edge Add-ons Store to install extensions automatically into Black Browser.</div>
    <div class='store-btns'>
      <a class='store-btn' href='https://chromewebstore.google.com'>🛒 Open Chrome Web Store</a>
      <a class='store-btn' href='https://microsoftedge.microsoft.com/addons'>🧩 Open Edge Add-ons Store</a>
    </div>
  </div>");
            }

            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<link href='https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&family=Inter:wght@400;500;600&display=swap' rel='stylesheet'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:'Segoe UI Variable Display','Plus Jakarta Sans','Inter',sans-serif;background:" + bg + @";color:" + textColor + @";padding:32px 24px;max-width:860px;margin:0 auto;-webkit-font-smoothing:antialiased}
.header{display:flex;align-items:center;justify-content:space-between;margin-bottom:28px;padding-bottom:16px;border-bottom:1px solid " + cardBorder + @"}
.title{font-size:26px;font-weight:700;display:flex;align-items:center;gap:10px}
.subtitle{font-size:14px;color:" + subTextColor + @";margin-top:4px}
.ext-list{display:flex;flex-direction:column;gap:16px}
.ext-card{display:flex;align-items:center;gap:18px;padding:20px;border-radius:16px;background:" + cardBg + @";border:1px solid " + cardBorder + @";box-shadow:0 4px 16px rgba(0,0,0,0.03)}
.ext-icon{width:48px;height:48px;border-radius:12px;background:rgba(0,103,192,0.1);color:#0067c0;display:flex;align-items:center;justify-content:center;font-size:24px;flex-shrink:0}
.ext-info{flex:1;min-width:0}
.ext-title{font-size:16px;font-weight:600;display:flex;align-items:center;gap:8px}
.ext-ver{font-size:12px;color:" + subTextColor + @";font-weight:400}
.ext-desc{font-size:13.5px;color:" + subTextColor + @";margin-top:4px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.ext-path{font-size:11.5px;color:" + subTextColor + @";opacity:0.7;margin-top:4px;font-family:monospace}
.status-pill{padding:5px 14px;border-radius:14px;background:rgba(0,103,192,0.12);color:#0067c0;font-size:12.5px;font-weight:600}
.empty-state{text-align:center;padding:60px 20px;background:" + cardBg + @";border-radius:20px;border:1px solid " + cardBorder + @"}
.empty-icon{font-size:54px;margin-bottom:14px}
.empty-title{font-size:20px;font-weight:600;margin-bottom:8px}
.empty-desc{font-size:14px;color:" + subTextColor + @";max-width:480px;margin:0 auto 24px}
.store-btns{display:flex;justify-content:center;gap:14px;flex-wrap:wrap}
.store-btn{padding:10px 22px;border-radius:20px;background:#0067c0;color:#fff;text-decoration:none;font-size:14px;font-weight:600;box-shadow:0 4px 14px rgba(0,103,192,0.3);transition:transform .15s}
.store-btn:hover{transform:scale(1.04)}
</style>
</head>
<body>

<div class='header'>
  <div>
    <div class='title'>🧩 Extensions Manager</div>
    <div class='subtitle'>Manage installed browser extensions from Chrome Web Store & Edge Add-ons</div>
  </div>
</div>

<div class='ext-list'>
" + listSb.ToString() + @"
</div>

</body>
</html>";
        }
    }
}
