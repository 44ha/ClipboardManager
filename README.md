# Clipboard Manager for Windows

A fast, lightweight Windows desktop clipboard manager built with C#, WPF, and .NET 8 that captures copied text and images, organizes history with instant search and pinning, and restores items with a customizable global hotkey.

---

Press `Ctrl + Shift + V` anywhere in Windows to toggle the clipboard history window.

---

## Features

- **Standard Desktop Experience**: Launches directly on your desktop, visible in the Windows taskbar, with standard minimize/close window controls.
- **Event-Driven Clipboard Monitoring**: Uses native Win32 `AddClipboardFormatListener` and `WM_CLIPBOARDUPDATE` messages to react immediately to clipboard updates without CPU-draining polling timers.
- **Text & Image Capture**: Automatically captures copied plain text and images (compressed losslessly into PNG blobs for low memory consumption).
- **Password Manager Privacy Exclusions**: Strictly honors the Windows `ExcludeClipboardContentFromMonitorProcessing` format (as well as `CanIncludeInClipboardHistory` and `Clipboard Viewer Ignore`) so credentials copied from Bitwarden, 1Password, or KeePass are never captured or stored.
- **Intelligent Deduplication**: Hashing content via SHA-256 prevents duplicate clutter; re-copying existing content automatically moves it to the top of the history list.
- **Search-as-you-type**: Instant client-side text filtering as you type in the search bar.
- **Favorite Pinning**: Pin critical snippets to keep them permanently at the top of your list; pinned items are never deleted during history limit pruning.
- **Global Hotkey**: Configurable shortcut (`Ctrl + Shift + V` by default) powered by Win32 `RegisterHotKey` to instantly bring the clipboard window to the front from anywhere in Windows.
- **Robust Persistence**: SQLite database stored in `%AppData%\ClipboardManager\clipboard.db` using EF Core, with application settings preserved in a JSON file that gracefully falls back to defaults if missing or corrupted.

---

## Tech Stack

- **Language / Runtime:** C# 12, .NET 8
- **UI Architecture:** WPF with **MVVM** pattern (`CommunityToolkit.Mvvm`)
- **Dependency Injection:** `Microsoft.Extensions.Hosting`
- **Database & ORM:** SQLite with **Entity Framework Core** (`Microsoft.EntityFrameworkCore.Sqlite`)
- **Native Interop (P/Invoke):** Win32 `RegisterHotKey`, `UnregisterHotKey`, `AddClipboardFormatListener`, `RemoveClipboardFormatListener`
- **Unit Testing:** xUnit + NSubstitute + SQLite In-Memory provider
- **CI / Automation:** GitHub Actions (`windows-latest`)

---

## Architecture Overview

The solution adheres strictly to clean architecture principles and separation of concerns:

```
ClipboardManager.sln
├── src/
│   ├── ClipboardManager.Core/        # Domain models, interfaces, and pure logic (zero WPF, zero EF Core dependencies)
│   ├── ClipboardManager.Data/        # EF Core DbContext, SQLite repository implementation, and JSON settings service
│   ├── ClipboardManager.Windows/     # Win32 P/Invoke interop: hotkey service, clipboard listener, and privacy filters
│   └── ClipboardManager.App/         # WPF presentation: Views, ViewModels, and DI host bootstrap
└── tests/
    ├── ClipboardManager.Core.Tests/  # Unit tests for domain models, SHA-256 hashing, and hotkey parsing
    └── ClipboardManager.Data.Tests/  # Unit tests against in-memory SQLite (deduplication, limits, pinning, search)
```

### Key Design Principles:
1. **Isolated Core Layer**: `ClipboardManager.Core` does not reference WPF, EF Core, or Win32 APIs. It defines domain contracts (`IClipboardHistoryRepository`, `IClipboardMonitor`, `IHotkeyService`, `ISettingsService`, `IClipboardPaster`).
2. **Lean ViewModels**: ViewModels communicate exclusively through domain service interfaces. They contain zero Win32 calls and zero direct database queries.
3. **Thread Safety & Scoped Contexts**: `SqliteClipboardHistoryRepository` utilizes `IDbContextFactory<ClipboardDbContext>` with an internal `SemaphoreSlim` lock, ensuring safe concurrent writes from background clipboard hooks while the UI queries history.
4. **No UI Freezes**: All database and I/O operations are fully asynchronous with `async/await` and `CancellationToken` support.
5. **Zero Memory Leaks**: Native Windows resources (`HWND`, hotkey registrations, clipboard format listeners) are strictly unhooked on exit via `IDisposable`.

---

## How to Build and Run

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.
- Windows 10 (1809+) or Windows 11.

### Build from Command Line
```powershell
# Restore NuGet dependencies
dotnet restore ClipboardManager.sln

# Build the solution in Release mode
dotnet build ClipboardManager.sln --configuration Release

# Run the full unit test suite
dotnet test ClipboardManager.sln --configuration Release
```

### Run the Application
```powershell
dotnet run --project src/ClipboardManager.App/ClipboardManager.App.csproj
```
When launched, press **`Ctrl + Shift + V`** to open the history window.

---

## Unit Testing Highlights

The test suite validates critical behavior against real in-memory SQLite instances:
- **Deduplication**: Re-copying an identical string or image moves the existing entry to the top without adding a redundant row.
- **Pinned Item Safety**: When the history count exceeds `MaxHistoryItems`, pinned items are **never** deleted.
- **FIFO Pruning for Non-Pinned Items**: Oldest non-pinned items are pruned first when capacity is exceeded.
- **Search Filtering**: Substring matching functions case-insensitively across text records.
- **Settings Fallback**: Corrupted or missing `settings.json` files seamlessly recover to safe default values without throwing exceptions.


