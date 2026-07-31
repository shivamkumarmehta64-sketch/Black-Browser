using System;
using System.IO;
using System.Text;

namespace BlackBrowser
{
    public static class SpeedDialPage
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

        public static string GetSpeedDialFilePath(bool isDarkMode)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "black-webview2");

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, "speeddial.html");

            string bg = isDarkMode
                ? "radial-gradient(circle at 50% 20%, #1c1d26 0%, #0f1016 100%)"
                : "radial-gradient(circle at 50% 20%, #ffffff 0%, #f3f5fa 100%)";

            string textColor = isDarkMode ? "#ffffff" : "#1d1d21";
            string subTextColor = isDarkMode ? "#9a9ab0" : "#6e6e82";
            string searchBg = isDarkMode ? "rgba(32, 33, 44, 0.88)" : "rgba(255, 255, 255, 0.95)";
            string searchBorder = isDarkMode ? "rgba(255, 255, 255, 0.14)" : "rgba(0, 0, 0, 0.08)";
            string cardBg = isDarkMode ? "rgba(255, 255, 255, 0.04)" : "rgba(255, 255, 255, 0.88)";
            string cardBorder = isDarkMode ? "rgba(255, 255, 255, 0.09)" : "rgba(255, 255, 255, 0.65)";

            StringBuilder dialsSb = new StringBuilder();
            try
            {
                var dials = CustomDialsManager.GetDials();
                if (dials != null && dials.Count > 0)
                {
                    foreach (var d in dials)
                    {
                        string safeTitle = SimpleHtmlEncode(d.Title ?? "App");
                        string safeUrl = SimpleHtmlEncode(d.Url ?? "https://google.com");
                        string safeIcon = SimpleHtmlEncode(d.IconText ?? "G");

                        dialsSb.Append(@"
  <a class='dial' href='" + safeUrl + @"'>
    <div class='dial-icon' style='background:" + (d.BgColor ?? "#e8f0fe") + @";color:" + (d.FgColor ?? "#1a73e8") + @"'>" + safeIcon + @"</div>
    <div class='dial-label'>" + safeTitle + @"</div>
  </a>");
                    }
                }
            }
            catch { }

            if (dialsSb.Length == 0)
            {
                dialsSb.Append(@"
  <a class='dial' href='https://www.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>G</div><div class='dial-label'>Google</div></a>
  <a class='dial' href='https://www.youtube.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>Y</div><div class='dial-label'>YouTube</div></a>
  <a class='dial' href='https://music.youtube.com'><div class='dial-icon' style='background:#fef7e0;color:#f29900'>M</div><div class='dial-label'>YT Music</div></a>
  <a class='dial' href='https://chromewebstore.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>🛒</div><div class='dial-label'>Chrome Store</div></a>");
            }

            string html = @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<link rel='preconnect' href='https://fonts.googleapis.com'>
<link rel='preconnect' href='https://fonts.gstatic.com' crossorigin>
<link href='https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@300;400;500;600;700&family=Outfit:wght@300;400;500;600;700&family=Inter:wght@400;500;600&display=swap' rel='stylesheet'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:'Segoe UI Variable Display','Plus Jakarta Sans','Inter',sans-serif;background:" + bg + @";color:" + textColor + @";display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:100vh;padding:36px 20px;overflow-x:hidden;-webkit-font-smoothing:antialiased}

.clock-container{text-align:center;margin-bottom:28px;animation:fadeIn 0.5s ease}
.time-display{font-size:76px;font-weight:300;letter-spacing:-2.8px;background:linear-gradient(135deg,#0067c0 0%,#0b57d0 100%);-webkit-background-clip:text;-webkit-text-fill-color:transparent;user-select:none;line-height:1.05;filter:drop-shadow(0 4px 14px rgba(0,103,192,0.18))}
.greeting{font-size:21px;font-weight:500;margin-top:8px;color:" + subTextColor + @";letter-spacing:-0.3px}
.ai-status-badge{display:inline-flex;align-items:center;gap:8px;margin-top:12px;padding:6px 18px;border-radius:20px;background:" + (isDarkMode ? "rgba(0,103,192,0.18)" : "rgba(0,103,192,0.08)") + @";color:#0067c0;font-size:12.5px;font-weight:600;border:1px solid rgba(0,103,192,0.22)}

.search-container{width:100%;max-width:680px;margin-bottom:40px;animation:fadeIn 0.7s ease}
.search-box{display:flex;align-items:center;width:100%;height:58px;padding:0 24px;border-radius:29px;background:" + searchBg + @";border:1.5px solid " + searchBorder + @";box-shadow:0 8px 32px rgba(0,0,0,0.06);backdrop-filter:blur(20px);transition:all .25s cubic-bezier(0.4,0,0.2,1)}
.search-box:hover,.search-box:focus-within{box-shadow:0 12px 40px rgba(0,103,192,0.25);border-color:#0067c0;transform:translateY(-1px)}
.search-icon{color:#0067c0;font-size:20px;margin-right:14px}
.search-box input{flex:1;background:transparent;border:none;outline:none;color:" + textColor + @";font-size:16.5px;font-weight:400;font-family:'Inter',sans-serif}
.search-box button{background:linear-gradient(135deg,#0067c0 0%,#0b57d0 100%);border:none;color:#ffffff;font-weight:600;font-size:14.5px;cursor:pointer;padding:0 26px;border-radius:22px;height:42px;box-shadow:0 4px 14px rgba(0,103,192,0.35);transition:all .15s ease}
.search-box button:hover{transform:scale(1.04);box-shadow:0 6px 20px rgba(0,103,192,0.45)}

.dials-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:20px;width:100%;max-width:680px;animation:fadeIn 0.9s ease}
.dial{display:flex;flex-direction:column;align-items:center;gap:10px;padding:18px 14px;border-radius:18px;background:" + cardBg + @";border:1px solid " + cardBorder + @";backdrop-filter:blur(20px);cursor:pointer;transition:all .22s cubic-bezier(0.4,0,0.2,1);text-decoration:none;color:" + textColor + @";box-shadow:0 4px 16px rgba(0,0,0,0.03)}
.dial:hover{transform:translateY(-5px) scale(1.04);border-color:#0067c0;box-shadow:0 14px 36px rgba(0,103,192,0.22)}
.dial-icon{width:52px;height:52px;border-radius:18px;display:flex;align-items:center;justify-content:center;font-size:22px;font-weight:700;box-shadow:0 4px 14px rgba(0,0,0,0.08);transition:transform .22s ease}
.dial:hover .dial-icon{transform:scale(1.08)}
.dial-label{font-size:13px;font-weight:600;letter-spacing:-0.1px;text-align:center;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:110px}

.features-bar{display:flex;align-items:center;justify-content:center;gap:14px;width:100%;max-width:680px;margin-top:32px;animation:fadeIn 1.1s ease;flex-wrap:wrap}
.feature-pill{display:inline-flex;align-items:center;gap:8px;padding:9px 18px;border-radius:20px;background:" + cardBg + @";border:1px solid " + cardBorder + @";backdrop-filter:blur(16px);color:" + textColor + @";font-size:13px;font-weight:600;cursor:pointer;transition:all .2s ease;text-decoration:none;box-shadow:0 2px 10px rgba(0,0,0,0.03)}
.feature-pill:hover{transform:translateY(-2px);border-color:#0067c0;box-shadow:0 8px 24px rgba(0,103,192,0.18);color:#0067c0}

.footer-note{margin-top:40px;font-size:12.5px;color:" + subTextColor + @";display:flex;align-items:center;gap:16px;background:" + (isDarkMode ? "rgba(255,255,255,0.03)" : "rgba(0,0,0,0.03)") + @";padding:10px 22px;border-radius:20px}

@keyframes fadeIn{from{opacity:0;transform:translateY(8px)}to{opacity:1;transform:translateY(0)}}
</style>
</head>
<body>

<div class='clock-container'>
  <div class='time-display' id='clock'>12:00 PM</div>
  <div class='greeting' id='greeting'>Welcome to Black Browser</div>
  <div class='ai-status-badge'>✨ Windows 11 Fluent 2 Design • 100% Local Privacy</div>
</div>

<form class='search-container' action='https://www.google.com/search' method='get'>
  <div class='search-box'>
    <span class='search-icon'>🔍</span>
    <input type='text' name='q' placeholder='Search Google or enter web address...' autofocus autocomplete='off'>
    <button type='submit'>Search</button>
  </div>
</form>

<div class='dials-grid'>
" + dialsSb.ToString() + @"
</div>

<div class='features-bar'>
  <a class='feature-pill' href='black://history'>📜 Local History</a>
  <a class='feature-pill' href='black://bookmarks'>⭐ Local Bookmarks</a>
  <a class='feature-pill' href='black://downloads'>📥 Local Downloads</a>
  <a class='feature-pill' href='https://chatgpt.com'>🤖 ChatGPT AI</a>
  <a class='feature-pill' href='https://chromewebstore.google.com'>🧩 Extensions</a>
</div>

<div class='footer-note'>
  <span>🔒 3-Layer Ad Shield</span>
  <span>•</span>
  <span>🕵️ Private Mode Ready</span>
  <span>•</span>
  <span>⚡ ~38MB RAM</span>
</div>

<script>
function updateClock() {
  var now = new Date();
  var h = now.getHours();
  var m = now.getMinutes();
  var ampm = h >= 12 ? 'PM' : 'AM';
  
  var greet = 'Welcome to Black Browser';
  if (h < 12) greet = 'Good Morning, Shiva — System Optimal';
  else if (h < 18) greet = 'Good Afternoon, Shiva — System Optimal';
  else greet = 'Good Evening, Shiva — System Optimal';

  h = h % 12; h = h ? h : 12;
  m = m < 10 ? '0' + m : m;
  
  document.getElementById('clock').innerText = h + ':' + m + ' ' + ampm;
  document.getElementById('greeting').innerText = greet;
}
updateClock();
setInterval(updateClock, 1000);
</script>

</body>
</html>";

            File.WriteAllText(filePath, html, Encoding.UTF8);
            return "file:///" + filePath.Replace("\\", "/");
        }

        public static string GetHtml(bool isDarkMode)
        {
            string path = GetSpeedDialFilePath(isDarkMode);
            return File.ReadAllText(path.Replace("file:///", ""), Encoding.UTF8);
        }
    }
}
