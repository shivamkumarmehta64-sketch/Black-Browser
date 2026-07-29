#![windows_subsystem = "windows"]

use std::cell::RefCell;
use std::rc::Rc;
use std::sync::OnceLock;

use adblock::engine::Engine;
use adblock::lists::{FilterSet, ParseOptions};
use adblock::request::Request;
use webview2_com::CoreWebView2EnvironmentOptions;
use webview2_com::{
    wait_with_pump, CreateCoreWebView2EnvironmentCompletedHandler,
    CreateCoreWebView2ControllerCompletedHandler,
    NavigationCompletedEventHandler, NavigationStartingEventHandler,
    WebMessageReceivedEventHandler, WebResourceRequestedEventHandler,
    Microsoft::Web::WebView2::Win32::*,
};
use windows::Win32::Foundation::*;
use windows::Win32::Graphics::Gdi::*;
use windows::Win32::System::Com::*;
use windows::Win32::System::LibraryLoader::*;
use windows::Win32::UI::Shell::*;
use windows::Win32::UI::WindowsAndMessaging::*;

const TRAY_ICON_ID: u32 = 1;

struct SafeController(ICoreWebView2Controller);
unsafe impl Send for SafeController {}
unsafe impl Sync for SafeController {}

static CONTROLLER: OnceLock<SafeController> = OnceLock::new();

struct AppState {
    engine: Option<Engine>,
}

unsafe extern "system" fn wndproc(
    hwnd: HWND,
    msg: u32,
    wparam: WPARAM,
    lparam: LPARAM,
) -> LRESULT {
    match msg {
        WM_SIZE => {
            if let Some(ctrl) = CONTROLLER.get() {
                let mut rect = RECT::default();
                GetClientRect(hwnd, &mut rect);
                let _ = ctrl.0.SetBounds(rect);
            }
            LRESULT(0)
        }
        WM_CLOSE => {
            ShowWindow(hwnd, SW_HIDE);
            LRESULT(0)
        }
        WM_DESTROY => {
            PostQuitMessage(0);
            LRESULT(0)
        }
        WM_APP => {
            let event = lparam.0 as u32;
            match event {
                0x0201 | 0x0203 => {
                    ShowWindow(hwnd, SW_SHOW);
                    SetForegroundWindow(hwnd);
                }
                0x0204 => {
                    if let Ok(menu) = CreatePopupMenu() {
                        let _ = AppendMenuW(menu, MF_STRING, 1, windows::core::w!("Show Black Noir"));
                        let _ = AppendMenuW(menu, MF_STRING, 2, windows::core::w!("Exit"));
                        let mut pt = POINT::default();
                        let _ = GetCursorPos(&mut pt);
                        SetForegroundWindow(hwnd);
                        let cmd = TrackPopupMenu(
                            menu,
                            TPM_RETURNCMD | TPM_RIGHTBUTTON,
                            pt.x,
                            pt.y,
                            Some(0),
                            hwnd,
                            None,
                        );
                        let _ = DestroyMenu(menu);
                        if cmd.0 == 2 {
                            remove_tray(hwnd);
                            DestroyWindow(hwnd);
                        }
                    }
                }
                _ => {}
            }
            LRESULT(0)
        }
        _ => DefWindowProcW(hwnd, msg, wparam, lparam),
    }
}

fn add_tray(hwnd: HWND) {
    unsafe {
        let mut nid = NOTIFYICONDATAW {
            cbSize: size_of::<NOTIFYICONDATAW>() as u32,
            hWnd: hwnd,
            uID: TRAY_ICON_ID,
            uFlags: NIF_ICON | NIF_TIP | NIF_MESSAGE,
            uCallbackMessage: WM_APP,
            ..Default::default()
        };
        let hicon = LoadIconW(None, IDI_APPLICATION).ok().unwrap_or_default();
        nid.hIcon = hicon;
        let tip: Vec<u16> = "Black Noir Browser\0".encode_utf16().collect();
        let len = tip.len().min(128);
        nid.szTip[..len].copy_from_slice(&tip[..len]);
        Shell_NotifyIconW(NIM_ADD, &mut nid);
    }
}

fn remove_tray(hwnd: HWND) {
    unsafe {
        let mut nid = NOTIFYICONDATAW {
            cbSize: size_of::<NOTIFYICONDATAW>() as u32,
            hWnd: hwnd,
            uID: TRAY_ICON_ID,
            ..Default::default()
        };
        Shell_NotifyIconW(NIM_DELETE, &mut nid);
    }
}

fn load_engine(exe_dir: &std::path::Path) -> Option<Engine> {
    let paths = [
        exe_dir.join("filters").join("combined.txt"),
        {
            let mut p = exe_dir.to_path_buf();
            p.pop();
            p.join("filters").join("combined.txt")
        },
        std::path::Path::new("C:\\Users\\shiva\\black-noir\\filters\\combined.txt").to_path_buf(),
    ];
    let text = paths.iter().find_map(|p| std::fs::read_to_string(p).ok());
    if let Some(text) = text {
        let mut fs = FilterSet::new(false);
        fs.add_filter_list(text, ParseOptions::default());
        Some(Engine::new_with_filter_set(fs))
    } else {
        eprintln!("no filters, ad blocking off");
        None
    }
}

fn main() {
    unsafe { let _ = CoInitializeEx(None, COINIT_APARTMENTTHREADED); }

    let inst = unsafe { GetModuleHandleW(None).ok().unwrap_or_default() };

    let wc = WNDCLASSEXW {
        cbSize: size_of::<WNDCLASSEXW>() as u32,
        style: CS_HREDRAW | CS_VREDRAW,
        lpfnWndProc: Some(wndproc),
        cbClsExtra: 0,
        cbWndExtra: 0,
        hInstance: inst.into(),
        hIcon: unsafe { LoadIconW(None, IDI_APPLICATION).ok().unwrap_or_default() },
        hCursor: unsafe { LoadCursorW(None, IDC_ARROW).ok().unwrap_or_default() },
        hbrBackground: unsafe { CreateSolidBrush(COLORREF(0x00101010)) },
        lpszMenuName: windows::core::PCWSTR::null(),
        lpszClassName: windows::core::w!("BlackNoirWindow"),
        hIconSm: unsafe { LoadIconW(None, IDI_APPLICATION).ok().unwrap_or_default() },
    };
    unsafe { let _ = RegisterClassExW(&wc); }

    let hwnd = unsafe {
        CreateWindowExW(
            WINDOW_EX_STYLE::default(),
            windows::core::w!("BlackNoirWindow"),
            windows::core::w!("Black Noir"),
            WS_OVERLAPPEDWINDOW | WS_VISIBLE,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            1200,
            800,
            None,
            None,
            Some(inst.into()),
            None,
        )
    }
    .unwrap();

    let exe_dir = std::env::current_exe()
        .ok()
        .and_then(|p| p.parent().map(|p| p.to_path_buf()))
        .unwrap_or_else(|| std::path::PathBuf::from("."));

    let state = Rc::new(RefCell::new(AppState {
        engine: load_engine(&exe_dir),
    }));

    let env_options: ICoreWebView2EnvironmentOptions =
        CoreWebView2EnvironmentOptions::default().into();
    let (env_tx, env_rx) = std::sync::mpsc::channel();
    let env_handler = CreateCoreWebView2EnvironmentCompletedHandler::create(Box::new(
        move |error_code, env| {
            if error_code.is_ok() {
                let _ = env_tx.send(env);
            }
            Ok(())
        },
    ));
    unsafe {
        let _ = CreateCoreWebView2EnvironmentWithOptions(
            Option::<&windows::core::PCWSTR>::None,
            Option::<&windows::core::PCWSTR>::None,
            &env_options,
            &env_handler,
        );
    }

    let env: ICoreWebView2Environment = match wait_with_pump(env_rx) {
        Ok(Some(env)) => env,
        _ => { eprintln!("env failed"); return; }
    };

    {
        let (ctrl_tx, ctrl_rx) = std::sync::mpsc::channel();
        let ctrl_handler = CreateCoreWebView2ControllerCompletedHandler::create(Box::new(
            move |error_code, controller| {
                if error_code.is_ok() {
                    let _ = ctrl_tx.send(controller);
                }
                Ok(())
            },
        ));
        unsafe { let _ = env.CreateCoreWebView2Controller(hwnd, &ctrl_handler); }
        let controller: ICoreWebView2Controller = match wait_with_pump(ctrl_rx) {
            Ok(Some(ctrl)) => ctrl,
            _ => { eprintln!("controller failed"); return; }
        };
        let _ = CONTROLLER.set(SafeController(controller.clone()));

        let mut rect = RECT::default();
        unsafe { GetClientRect(hwnd, &mut rect); let _ = controller.SetBounds(rect); }

        let webview = unsafe { controller.CoreWebView2() }.expect("CoreWebView2");

        let s = state.clone();
        let nav = NavigationStartingEventHandler::create(Box::new(move |_, args| {
            if let Some(a) = args {
                let g = s.borrow();
                if let Some(ref e) = g.engine {
                    unsafe {
                        let mut u = windows::core::PWSTR::null();
                        if a.Uri(&mut u).is_ok() {
                            let uri = webview2_com::take_pwstr(u);
                            if !uri.is_empty() {
                                if let Ok(r) = Request::new(&uri, &uri, "document", "GET") {
                                    if e.check_network_request(&r).should_block() {
                                        let _ = a.SetCancel(true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Ok(())
        }));
        unsafe {
            let mut t = 0i64;
            let _ = webview.add_NavigationStarting(&nav, &mut t);
        }

        let s = state.clone();
        let ec = env.clone();
        let res = WebResourceRequestedEventHandler::create(Box::new(move |_, args| {
            if let Some(a) = args {
                let g = s.borrow();
                if let Some(ref e) = g.engine {
                    unsafe {
                        if let Ok(r) = a.Request() {
                            let mut u = windows::core::PWSTR::null();
                            if r.Uri(&mut u).is_ok() {
                                let uri = webview2_com::take_pwstr(u);
                                if !uri.is_empty() {
                                    let mut ctx = COREWEBVIEW2_WEB_RESOURCE_CONTEXT(0);
                                    let _ = a.ResourceContext(&mut ctx);
                                    let rt = match ctx {
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_DOCUMENT => "document",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_SCRIPT => "script",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_IMAGE => "image",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_STYLESHEET => "stylesheet",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_FONT => "font",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_MEDIA => "media",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_XML_HTTP_REQUEST => "xmlhttprequest",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_FETCH => "fetch",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_WEBSOCKET => "websocket",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_PING => "beacon",
                                        COREWEBVIEW2_WEB_RESOURCE_CONTEXT_CSP_VIOLATION_REPORT => "csp_report",
                                        _ => "other",
                                    };
                                    if let Ok(req) = Request::new(&uri, "", rt, "GET") {
                                        if e.check_network_request(&req).should_block() {
                                            if let Ok(rsp) = ec.CreateWebResourceResponse(
                                                Option::<&IStream>::None,
                                                204,
                                                windows::core::PCWSTR::null(),
                                                windows::core::PCWSTR::null(),
                                            ) {
                                                let _ = a.SetResponse(&rsp);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Ok(())
        }));
        unsafe {
            let _ = webview.AddWebResourceRequestedFilter(
                windows::core::w!("*"),
                COREWEBVIEW2_WEB_RESOURCE_CONTEXT_ALL,
            );
            let mut t = 0i64;
            let _ = webview.add_WebResourceRequested(&res, &mut t);
        }

        let msg_h = WebMessageReceivedEventHandler::create(Box::new(move |_, args| {
            if let Some(a) = args {
                unsafe {
                    let mut m = windows::core::PWSTR::null();
                    if a.TryGetWebMessageAsString(&mut m).is_ok() {
                        let s = webview2_com::take_pwstr(m);
                        if s == "quit" {
                            remove_tray(hwnd);
                            PostQuitMessage(0);
                        }
                    }
                }
            }
            Ok(())
        }));
        unsafe {
            let mut t = 0i64;
            let _ = webview.add_WebMessageReceived(&msg_h, &mut t);
        }

        let wv = webview.clone();
        let js = include_str!("../web/inject.js");
        let nav_c = NavigationCompletedEventHandler::create(Box::new(move |_, _| {
            unsafe {
                let w: Vec<u16> = js.encode_utf16().chain(std::iter::once(0)).collect();
                let _ = wv.ExecuteScript(
                    windows::core::PCWSTR(w.as_ptr()),
                    Option::<&ICoreWebView2ExecuteScriptCompletedHandler>::None,
                );
            }
            Ok(())
        }));
        unsafe {
            let mut t = 0i64;
            let _ = webview.add_NavigationCompleted(&nav_c, &mut t);
        }

        let html = include_str!("../web/index.html");
        unsafe {
            let w: Vec<u16> = html.encode_utf16().chain(std::iter::once(0)).collect();
            let _ = webview.NavigateToString(windows::core::PCWSTR(w.as_ptr()));
        }
    }

    add_tray(hwnd);

    let mut msg = MSG::default();
    unsafe {
        while GetMessageW(&mut msg, None, 0, 0).as_bool() {
            let _ = TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
    }

    remove_tray(hwnd);
    unsafe { CoUninitialize(); }
}
