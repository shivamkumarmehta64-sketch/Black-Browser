namespace BlackBrowser
{
    public static class SpeedDialPage
    {
        public static string GetHtml(bool isDarkMode)
        {
            string bg = isDarkMode ? "#121216" : "linear-gradient(180deg,#ffffff 0%,#f5f5f7 100%)";
            string textColor = isDarkMode ? "#ffffff" : "#1d1d1f";
            string searchBg = isDarkMode ? "#202025" : "#ffffff";
            string searchBorder = isDarkMode ? "#303038" : "#dfe1e5";

            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,Ubuntu,sans-serif;background:" + bg + @";color:" + textColor + @";display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:100vh;padding:40px 20px;overflow-x:hidden;-webkit-font-smoothing:antialiased}

.clock-container{text-align:center;margin-bottom:28px}
.time-display{font-size:62px;font-weight:300;letter-spacing:-1.5px;color:#1a73e8;user-select:none;line-height:1}
.greeting{font-size:20px;font-weight:500;margin-top:10px;color:#80868b}

.search-container{width:100%;max-width:620px;margin-bottom:44px}
.search-box{display:flex;align-items:center;width:100%;height:54px;padding:0 22px;border-radius:27px;background:" + searchBg + @";border:1.5px solid " + searchBorder + @";box-shadow:0 4px 20px rgba(0,0,0,0.06);transition:all .25s ease}
.search-box:hover,.search-box:focus-within{box-shadow:0 8px 30px rgba(26,115,232,0.22);border-color:#1a73e8}
.search-icon{color:#1a73e8;font-size:19px;margin-right:14px}
.search-box input{flex:1;background:transparent;border:none;outline:none;color:" + textColor + @";font-size:16.5px;font-weight:400}
.search-box button{background:linear-gradient(135deg,#1a73e8 0%,#0b57d0 100%);border:none;color:#ffffff;font-weight:600;font-size:14.5px;cursor:pointer;padding:0 24px;border-radius:20px;height:40px;box-shadow:0 3px 12px rgba(26,115,232,0.3);transition:all .15s ease}
.search-box button:hover{transform:scale(1.03);box-shadow:0 6px 18px rgba(26,115,232,0.4)}

.dials-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:24px;width:100%;max-width:620px}
.dial{display:flex;flex-direction:column;align-items:center;gap:12px;padding:18px;border-radius:18px;background:" + (isDarkMode ? "rgba(255,255,255,0.04)" : "rgba(255,255,255,0.9)") + @";border:1px solid " + (isDarkMode ? "rgba(255,255,255,0.08)" : "rgba(0,0,0,0.06)") + @";backdrop-filter:blur(16px);cursor:pointer;transition:all .2s ease;text-decoration:none;color:" + textColor + @";box-shadow:0 2px 10px rgba(0,0,0,0.04)}
.dial:hover{transform:translateY(-5px) scale(1.04);border-color:#1a73e8;box-shadow:0 14px 34px rgba(26,115,232,0.18)}
.dial-icon{width:54px;height:54px;border-radius:18px;display:flex;align-items:center;justify-content:center;font-size:23px;font-weight:700;box-shadow:0 2px 10px rgba(0,0,0,0.08);transition:transform .2s ease}
.dial:hover .dial-icon{transform:scale(1.08)}
.dial-label{font-size:13px;font-weight:600}

.footer-note{margin-top:52px;font-size:12.5px;color:#80868b;display:flex;align-items:center;gap:18px;background:" + (isDarkMode ? "rgba(255,255,255,0.03)" : "rgba(0,0,0,0.03)") + @";padding:10px 24px;border-radius:20px}
</style>
</head>
<body>

<div class='clock-container'>
  <div class='time-display' id='clock'>12:00 PM</div>
  <div class='greeting' id='greeting'>Welcome to Black Browser</div>
</div>

<form class='search-container' action='https://www.google.com/search' method='get'>
  <div class='search-box'>
    <span class='search-icon'>🔍</span>
    <input type='text' name='q' placeholder='Search Google or enter web address...' autofocus autocomplete='off'>
    <button type='submit'>Search</button>
  </div>
</form>

<div class='dials-grid'>
  <a class='dial' href='https://www.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>G</div><div class='dial-label'>Google</div></a>
  <a class='dial' href='https://www.youtube.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>Y</div><div class='dial-label'>YouTube</div></a>
  <a class='dial' href='https://music.youtube.com'><div class='dial-icon' style='background:#fef7e0;color:#f29900'>M</div><div class='dial-label'>YT Music</div></a>
  <a class='dial' href='https://chromewebstore.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>🛒</div><div class='dial-label'>Chrome Store</div></a>
  <a class='dial' href='https://github.com'><div class='dial-icon' style='background:#e8eaed;color:#202124'>GH</div><div class='dial-label'>GitHub</div></a>
  <a class='dial' href='https://reddit.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>R</div><div class='dial-label'>Reddit</div></a>
  <a class='dial' href='https://chatgpt.com'><div class='dial-icon' style='background:#e6f4ea;color:#107c41'>AI</div><div class='dial-label'>ChatGPT</div></a>
  <a class='dial' href='https://microsoftedge.microsoft.com/addons'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>🧩</div><div class='dial-label'>Edge Add-ons</div></a>
</div>

<div class='footer-note'>
  <span>🔒 3-Layer Ad Shield Active</span>
  <span>•</span>
  <span>👁️ Eye Care Ready</span>
  <span>•</span>
  <span>⚡ ~40MB Tray RAM</span>
</div>

<script>
function updateClock() {
  var now = new Date();
  var h = now.getHours();
  var m = now.getMinutes();
  var ampm = h >= 12 ? 'PM' : 'AM';
  
  var greet = 'Welcome to Black Browser';
  if (h < 12) greet = 'Good Morning — Welcome to Black';
  else if (h < 18) greet = 'Good Afternoon — Welcome to Black';
  else greet = 'Good Evening — Welcome to Black';

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
        }
    }
}
