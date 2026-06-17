# Development Plan: Secure Exam Browser for Windows (C# WPF)

This document outlines the step-by-step development phases for the minimalist lockdown exam browser. The AI Agent must guide the developer through one phase at a time, ensuring complete understanding before moving to the next.

---

## 📋 Phase Roadmap Overview

| Phase       | Title              | Core Objective                                     | Key Technology/Concepts                |
| :---------- | :----------------- | :------------------------------------------------- | :------------------------------------- |
| **Phase 1** | The Skeleton       | Create a blank fullscreen, borderless window.      | WPF Windows Configuration              |
| **Phase 2** | The WebView        | Integrate Chromium browser to load the exam URL.   | Microsoft Edge WebView2 NuGet          |
| **Phase 3** | The Lockdown       | Block system shortcuts (Alt+Tab, Win Key, etc.).   | Windows API Hooks (`SetWindowsHookEx`) |
| **Phase 4** | The Security Layer | Implement Admin Password prompt on exit attempt.   | WPF Window Closing Events & Dialogs    |
| **Phase 5** | The Deployment     | Compile into a self-contained single-file executable (no .NET install needed). | `dotnet publish` CLI                   |

---

## 🔍 Detailed Phase Specifications

### 🟦 Phase 1: Fondasi Jendela (The Skeleton)

- **Goal:** Establish a completely un-interruptible, full-screen blank window canvas.
- **Requirements:**
  - Set window to maximum resolution without borders (`WindowStyle="None"`, `WindowState="Maximized"`).
  - Prevent window resizing (`ResizeMode="NoResize"`).
  - Force the window to stay on top of everything (`Topmost="True"`).
  - Hide standard OS close/minimize decorations.

### 🟦 Phase 2: Integrasi Browser (The WebView)

- **Goal:** Embed a secure, modern web browser component inside the Phase 1 window.
- **Requirements:**
  - Add the `Microsoft.Web.WebView2` package via dotnet CLI.
  - Initialize WebView2 asynchronously (`EnsureCoreWebView2Async`).
  - Configure WebView2 settings: Disable right-click context menus, disable DevTools (`F12`), and disable zoom controls.
  - Point the browser to a configurable placeholder URL (e.g., `https://web-ujian-kamu.com`).

### 🟦 Phase 3: Penguncian & Keamanan (The Lockdown)

- **Goal:** Intercept and neutralize Windows OS-level keyboard shortcuts that allow cheating.
- **Requirements:**
  - Implement Low-Level Keyboard Hooks using Windows Win32 API (`user32.dll`).
  - Intercept and block: `Alt + Tab`, `LWin / RWin` (Start Menu), `Alt + F4` (Force Quit), and `Ctrl + Esc`.
  - Ensure proper resource cleanup (`UnhookWindowsHookEx`) when the application terminates to prevent OS lag.

### 🟦 Phase 4: Mekanisme Keluar & Keamanan Tambahan (The Security Layer)

- **Goal:** Create an exit gateway protected by an administrator password.
- **Requirements:**
  - Intercept the window `Closing` event to prevent standard termination.
  - Launch a custom modal input box requesting an Admin Password (Default: `Admin123!`).
  - Allow the app to close _only_ if the password matches; otherwise, cancel the exit event.

### 🟦 Phase 5: Finalisasi & Build (The Deployment)

- **Goal:** Compile the C# source code into a self-contained, single-file executable that runs on any Windows 10/11 machine WITHOUT requiring a separate .NET installation.
- **Requirements:**
  - Use the `dotnet publish` CLI command targeted for `win-x64`.
  - Apply **Self-Contained** deployment (`--self-contained true`) so the .NET 8 runtime is bundled inside the `.exe`. Larger file size (~70MB - 150MB) is an accepted trade-off for zero-dependency, plug-and-play distribution.
  - Bundle everything into one standalone `.exe` (`/p:PublishSingleFile=true`). Optionally enable compression (`/p:EnableCompressionInSingleFile=true`) to reduce size.

---

## 🤖 Instructions for AI Agent

1. **Strict Line-by-Line Explanation:** For every piece of C# or XAML code generated, provide a clear, beginner-friendly explanation of what each line/attribute does.
2. **One Phase at a Time:** Do not implement features from future phases unless explicitly instructed by the developer.
3. **CLI-First Guidance:** Since the developer is on macOS using VS Code, always provide terminal-based commands (`dotnet`) instead of Visual Studio GUI navigation paths.
