# AI Agent Instructions: Simple Ujian Browser for Windows (C# WPF)

## 1. Project Overview & Goal

You are an expert Senior C# .NET Developer. Your task is to assist in building a lightweight, highly secure, minimalist lockdown browser (Safe Browser Exam clone) for Windows.

The application will act as a secure "wrapper" around a web-based examination platform. It must prevent students from navigating away, opening other apps, or using unauthorized keyboard shortcuts during the exam.

## 2. Technical Stack & Environment

- **Language:** C#
- **Framework:** .NET 8.0 or .NET 9.0 (WPF - Windows Presentation Foundation)
- **Web Component:** Microsoft Edge WebView2 (Chromium-based)
- **Target OS:** Windows 10 and Windows 11
- **Deployment Goal:** Self-Contained Single-File Executable. The `.NET 8 runtime is bundled INSIDE the `.exe` so target laptops do NOT need .NET installed. File size is larger (~70MB - 150MB) and this trade-off is acceptable in exchange for zero-dependency, plug-and-play distribution to student machines.

### ⚠️ IMPORTANT: Development Workflow Notes for the Agent

- **Host OS:** The developer is writing code on **macOS using VS Code (with C# Dev Kit)**.
- **Testing OS:** The application cannot be run or tested on macOS. Testing and final compilation will be done on a **separate Windows machine via CLI**.
- **Agent Guidance:** Do NOT instruct the developer to use Visual Studio GUI tools (like the XAML Designer, Properties Window, or Visual Studio Publish Wizard). All code instructions must be tailored for raw code editing in VS Code, and all compilation instructions must use the `dotnet` CLI.

## 3. Core Functional Requirements

### A. Window & UI Configuration

- The application must launch immediately in **True Fullscreen** and **Kiosk Mode**.
- Set `WindowStyle="None"`, `ResizeMode="NoResize"`, and `WindowState="Maximized"`.
- Implement `Topmost="True"` to ensure it stays above all other system windows (including basic notifications).
- Hide the Windows Taskbar and prevent the application from losing focus.

### B. WebView2 Integration

- Embed a full-screen `WebView2` control inside the main window.
- The default initial URL must be configurable (Placeholder: `https://web-ujian-kamu.com`).
- Disable the default WebView2 context menu (Right-Click).
- Disable DevTools access (`Ctrl+Shift+I` or `F12`).
- Disable built-in browser status bars, acceleration keys, and zoom features.

### C. Security & Lockdown (Low-Level Hooks)

- Implement Windows API Hooks (`SetWindowsHookEx`) to intercept and block the following system-level shortcuts:
  - `Alt + Tab` (App Switching)
  - `Windows Key` / `LWin` / `RWin` (Start Menu)
  - `Alt + F4` (Force Close)
  - `Ctrl + Esc` (Start Menu)
- _Note on Task Manager / Ctrl+Alt+Del:_ These **cannot** be blocked from a user-mode app. `Ctrl+Alt+Del` is a Secure Attention Sequence handled below all applications and switches to a separate secure desktop. `Topmost="True"` is **NOT** a security boundary — it only orders above non-topmost windows, and once our process is ended (via Task Manager reached through Ctrl+Alt+Del) the window is gone. On BYOD this lockdown is **deterrence, not a guarantee**. See [plan/PLAN2.md](plan/PLAN2.md).

### D. Admin Exit Mechanism

- Intercept the window closing event.
- If a user attempts to close the app, prompt them with a custom native dialog asking for an **Admin Password**.
- The password is verified against a **PBKDF2 hash** taken from `config.json` (Firebase Hosting); see `PasswordHasher.cs` / `ConfigService.cs`. Fallback when config has no `adminPassword`: `Admin123!`.
- The application can only exit if the correct password is provided.

## 4. Coding & Architecture Guidelines

- **Clean Code:** Write clean, asynchronous, and well-commented C# code.
- **Safety First:** Avoid memory leaks when registering low-level keyboard hooks; ensure proper unhooking when the application closes (`UnhookWindowsHookEx`).
- **WebView2 Readiness:** Ensure `EnsureCoreWebView2Async()` is fully awaited before attempting to manipulate browser settings or navigating to the URL.
- **Inter-Process Communication (IPC):** Prepare the code structure for Web-to-Native communication using `window.chrome.webview.postMessage` in case the web frontend needs to signal a security breach.

## 5. Your Next Steps as the AI Agent

When asked to write code, please provide:

1. The exact XAML layout for `MainWindow.xaml`.
2. The complete C# code-behind for `MainWindow.xaml.cs` containing the window initialization, WebView2 setup, and low-level keyboard hook logic.
3. Detailed explanations on how to safely compile the project into a **self-contained single-file `.exe`** using the `dotnet publish` CLI command for Windows (`--self-contained true -r win-x64 /p:PublishSingleFile=true`), so the output runs on any Windows 10/11 machine without requiring a separate .NET installation.
