mod filter;

use tauri::{Manager, WebviewUrl, WebviewWindowBuilder, AppHandle};

#[tauri::command]
fn navigate_to(app: AppHandle, url: String) -> Result<(), String> {
    if let Some(w) = app.get_webview_window("main") {
        let parsed: url::Url = if !url.starts_with("http://") && !url.starts_with("https://") {
            format!("https://{}", url).parse().or(Err("Invalid URL"))?
        } else {
            url.parse().or(Err("Invalid URL"))?
        };
        let _ = w.navigate(parsed);
    }
    Ok(())
}

#[tauri::command]
fn go_back(app: AppHandle) -> Result<(), String> {
    if let Some(w) = app.get_webview_window("main") {
        let _ = w.eval("window.history.back()");
    }
    Ok(())
}

#[tauri::command]
fn go_forward(app: AppHandle) -> Result<(), String> {
    if let Some(w) = app.get_webview_window("main") {
        let _ = w.eval("window.history.forward()");
    }
    Ok(())
}

#[tauri::command]
fn refresh_page(app: AppHandle) -> Result<(), String> {
    if let Some(w) = app.get_webview_window("main") {
        let _ = w.eval("location.reload()");
    }
    Ok(())
}

#[tauri::command]
fn quit_app(app: AppHandle) -> Result<(), String> {
    app.exit(0);
    Ok(())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let inj_js = include_str!("../../inject.js");

    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            navigate_to, go_back, go_forward, refresh_page, quit_app,
        ])
        .setup(move |app| {
            let inj = inj_js.to_string();
            let _window = WebviewWindowBuilder::new(app, "main",
                WebviewUrl::App("index.html".into()))
                .title("Black Noir")
                .inner_size(1280.0, 800.0)
                .min_inner_size(600.0, 400.0)
                .user_agent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36")
                .on_navigation(|url| {
                    !filter::is_ad_url(&url)
                })
                .on_page_load(move |wv, payload| {
                    use tauri::webview::PageLoadEvent;
                    if payload.event() == PageLoadEvent::Started {
                        let _ = wv.eval(&inj);
                    }
                })
                .build()
                .expect("failed to build window");
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
