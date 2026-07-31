using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

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
            string wallpaperPath = Path.Combine(Application.StartupPath, "wallpaper.jpg");

            string bgImageStyle = "";
            if (File.Exists(wallpaperPath))
            {
                bgImageStyle = "background-image: linear-gradient(to bottom, rgba(11, 13, 27, 0.72), rgba(15, 16, 26, 0.88)), url('" + wallpaperPath.Replace("\\", "/") + "'); background-size: cover; background-position: center; background-attachment: fixed;";
            }
            else
            {
                bgImageStyle = "background: radial-gradient(circle at 50% 20%, #1a1c2e 0%, #0b0d1b 100%);";
            }

            string textColor = "#ffffff";
            string subTextColor = "#a2a6cc";
            string searchBg = "rgba(22, 25, 46, 0.75)";
            string searchBorder = "rgba(123, 97, 255, 0.35)";
            string cardBg = "rgba(255, 255, 255, 0.06)";
            string cardBorder = "rgba(255, 255, 255, 0.12)";

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
    <div class='dial-icon' style='background:" + (d.BgColor ?? "#1a73e8") + @";color:#ffffff'>" + safeIcon + @"</div>
    <div class='dial-label'>" + safeTitle + @"</div>
  </a>");
                    }
                }
            }
            catch { }

            if (dialsSb.Length == 0)
            {
                dialsSb.Append(@"
  <a class='dial' href='https://www.google.com'><div class='dial-icon' style='background:linear-gradient(135deg, #4285F4, #34A853)'>G</div><div class='dial-label'>Google</div></a>
  <a class='dial' href='https://gemini.google.com'><div class='dial-icon' style='background:linear-gradient(135deg, #7B61FF, #4285F4)'>✨</div><div class='dial-label'>Gemini AI</div></a>
  <a class='dial' href='https://www.youtube.com'><div class='dial-icon' style='background:linear-gradient(135deg, #FF4B4B, #FF416C)'>▶</div><div class='dial-label'>YouTube</div></a>
  <a class='dial' href='https://music.youtube.com'><div class='dial-icon' style='background:linear-gradient(135deg, #F29900, #FF512F)'>🎵</div><div class='dial-label'>YT Music</div></a>
  <a class='dial' href='https://chromewebstore.google.com'><div class='dial-icon' style='background:linear-gradient(135deg, #00C6FF, #0072FF)'>🛒</div><div class='dial-label'>Chrome Store</div></a>");
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
body{" + bgImageStyle + @"font-family:'Segoe UI Variable Display','Plus Jakarta Sans','Inter',sans-serif;color:" + textColor + @";display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:100vh;padding:36px 20px;overflow-x:hidden;-webkit-font-smoothing:antialiased}

.clock-container{text-align:center;margin-bottom:32px;animation:fadeIn 0.5s ease}
.gemini-logo{width:64px;height:64px;margin-bottom:12px;filter:drop-shadow(0 0 20px rgba(123,97,255,0.6));animation:pulse 3s infinite ease-in-out}
.time-display{font-size:82px;font-weight:300;letter-spacing:-3px;background:linear-gradient(135deg, #4285F4 0%, #9B51E0 50%, #FF416C 100%);-webkit-background-clip:text;-webkit-text-fill-color:transparent;user-select:none;line-height:1.05;filter:drop-shadow(0 6px 24px rgba(123,97,255,0.35))}
.greeting{font-size:22px;font-weight:500;margin-top:10px;color:" + subTextColor + @";letter-spacing:-0.3px}
.ai-status-badge{display:inline-flex;align-items:center;gap:8px;margin-top:14px;padding:8px 22px;border-radius:24px;background:rgba(123,97,255,0.18);color:#a78bfa;font-size:13px;font-weight:600;border:1px solid rgba(123,97,255,0.35);backdrop-filter:blur(12px);box-shadow:0 0 20px rgba(123,97,255,0.2)}

.search-container{width:100%;max-width:700px;margin-bottom:44px;animation:fadeIn 0.7s ease}
.search-box{display:flex;align-items:center;width:100%;height:62px;padding:0 26px;border-radius:31px;background:" + searchBg + @";border:1.5px solid " + searchBorder + @";box-shadow:0 8px 36px rgba(0,0,0,0.3);backdrop-filter:blur(24px);transition:all .25s cubic-bezier(0.4,0,0.2,1)}
.search-box:hover,.search-box:focus-within{box-shadow:0 12px 48px rgba(123,97,255,0.45);border-color:#7B61FF;transform:translateY(-1px)}
.search-icon{color:#7B61FF;font-size:22px;margin-right:16px}
.search-box input{flex:1;background:transparent;border:none;outline:none;color:" + textColor + @";font-size:17px;font-weight:400;font-family:'Inter',sans-serif}
.search-box button{background:linear-gradient(135deg, #4285F4 0%, #7B61FF 100%);border:none;color:#ffffff;font-weight:600;font-size:15px;cursor:pointer;padding:0 28px;border-radius:24px;height:44px;box-shadow:0 4px 18px rgba(123,97,255,0.4);transition:all .15s ease}
.search-box button:hover{transform:scale(1.04);box-shadow:0 6px 24px rgba(123,97,255,0.55)}

.dials-grid{display:grid;grid-template-columns:repeat(5,1fr);gap:20px;width:100%;max-width:720px;animation:fadeIn 0.9s ease}
.dial{display:flex;flex-direction:column;align-items:center;gap:10px;padding:18px 14px;border-radius:20px;background:" + cardBg + @";border:1px solid " + cardBorder + @";backdrop-filter:blur(24px);cursor:pointer;transition:all .22s cubic-bezier(0.4,0,0.2,1);text-decoration:none;color:" + textColor + @";box-shadow:0 4px 20px rgba(0,0,0,0.15)}
.dial:hover{transform:translateY(-6px) scale(1.04);border-color:#7B61FF;box-shadow:0 16px 40px rgba(123,97,255,0.3)}
.dial-icon{width:54px;height:54px;border-radius:20px;display:flex;align-items:center;justify-content:center;font-size:24px;font-weight:700;box-shadow:0 4px 16px rgba(0,0,0,0.2);transition:transform .22s ease}
.dial:hover .dial-icon{transform:scale(1.08)}
.dial-label{font-size:13px;font-weight:600;letter-spacing:-0.1px;text-align:center;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:110px}

.features-bar{display:flex;align-items:center;justify-content:center;gap:14px;width:100%;max-width:720px;margin-top:36px;animation:fadeIn 1.1s ease;flex-wrap:wrap}
.feature-pill{display:inline-flex;align-items:center;gap:8px;padding:10px 20px;border-radius:22px;background:" + cardBg + @";border:1px solid " + cardBorder + @";backdrop-filter:blur(20px);color:" + textColor + @";font-size:13.5px;font-weight:600;cursor:pointer;transition:all .2s ease;text-decoration:none;box-shadow:0 2px 12px rgba(0,0,0,0.1)}
.feature-pill:hover{transform:translateY(-2px);border-color:#7B61FF;box-shadow:0 8px 28px rgba(123,97,255,0.3);color:#a78bfa}

.footer-note{margin-top:44px;font-size:12.5px;color:" + subTextColor + @";display:flex;align-items:center;gap:16px;background:rgba(255,255,255,0.04);padding:12px 24px;border-radius:24px;backdrop-filter:blur(16px);border:1px solid rgba(255,255,255,0.08)}

@keyframes fadeIn{from{opacity:0;transform:translateY(10px)}to{opacity:1;transform:translateY(0)}}
@keyframes pulse{0%,100%{transform:scale(1)}50%{transform:scale(1.06)}}
</style>
</head>
<body>

<div class='clock-container'>
  <div class='time-display' id='clock'>12:00 PM</div>
  <div class='greeting' id='greeting'>Welcome to Black Browser</div>
  <div class='ai-status-badge'>✨ Google Gemini Multicolor Theme • 100% Privacy</div>
</div>

<form class='search-container' action='https://www.google.com/search' method='get'>
  <div class='search-box'>
    <span class='search-icon'>✨</span>
    <input type='text' name='q' placeholder='Ask Google Gemini or search the web...' autofocus autocomplete='off'>
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
  <a class='feature-pill' href='black://extensions'>🧩 Extensions</a>
  <a class='feature-pill' href='https://gemini.google.com'>✨ Gemini AI</a>
</div>

<div class='footer-note'>
  <span>🔒 3-Layer AdShield</span>
  <span>•</span>
  <span>🕵️ Private Mode</span>
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
  if (h < 12) greet = 'Good Morning, Shiva — Google Gemini Theme Active';
  else if (h < 18) greet = 'Good Afternoon, Shiva — Google Gemini Theme Active';
  else greet = 'Good Evening, Shiva — Google Gemini Theme Active';

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
