# Development & release notes

## Layout

| Path | Purpose |
|---|---|
| `src/` | WPF/WebView2 host, JavaScript controller, styles and localization |
| `ui/` | Original vector reference and the HTML layout template |
| `scripts/` | Verified dependency bootstrap, build and packaging |
| `installer/` | Inno Setup definition and welcome text |
| `tests/` | UI, native-window, launcher and screenshot probes |
| `docs/images/` | Real product screenshots and rendered cover images |
| `licenses/` | Redistributed third-party license texts |

## Build

Use Windows x64, PowerShell 7 and Node.js. The .NET Framework C# compiler is taken from the installed Framework directory.

```powershell
pwsh .\scripts\build.ps1 -Installer
```

The runtime versions and SHA-256 hashes are pinned in `scripts/dependencies.lock.json`. Downloads are cached in `.cache/`. Python is embedded, and wheels are extracted directly rather than installed into your global Python environment. Inno Setup is installed under `.tools/` for installer builds.

Build outputs:

```text
dist/
  Translate-1.0.0-Windows-x64/      # local compilation output
  Translate-1.0.0-Portable-x64.zip  # clean, allowlisted staging tree
  Translate-1.0.0-Setup-x64.exe
  SHA256SUMS.txt
```

The portable ZIP contains a `Translate/` folder. Installer and ZIP use the same staged files under `.cache/package/Translate/`. Local `settings.dat`, `WebView2Data`, recordings, model weights, `.pyc` files and diagnostic executables never enter that staging tree.

## Tests

Generate the production HTML, then run the JavaScript interaction test in headless Edge:

```powershell
Push-Location src
node prepare-ui.cjs
node ..\tests\ui-smoke.cjs
Pop-Location
```

`VerifyQuickLaunch.cs` exercises the real WebView2 host: opening and choosing every language, incoming results during an open menu, long text, blue selection dots, native visibility, popup bounds, Esc and warm reopening. It uses test translation responses and does not need a key. Constructing `MainWindow(false)` disables installation of the background launcher for this probe.

`VerifyLauncher.cs` uses a workspace-only startup shortcut and a test executable to check cold launch and existing-process signaling. It does not modify the real startup shortcut.

The app also accepts `--self-test <report-path>` for Offline/parser validation. Put test models in a `model-cache/` directory beside the report. Offline model weights are not checked into Git.

`CaptureScreenshots.cs` captures the compiled app with an English interface. The demo sentence is produced by the actual Offline engine. No API key is used. `node scripts/render-brand.cjs` creates cover PNGs from the original HTML artwork and those screenshots.

## Publishing

1. Build both distributions and inspect their file lists.
2. Verify source files and image metadata contain no secrets or personal data.
3. Test Setup installation/uninstallation in a disposable destination.
4. Commit reviewed source, documentation and images; exclude `dist/`, `.cache/` and `.tools/`.
5. Create a tagged GitHub Release and attach Setup, Portable ZIP and `SHA256SUMS.txt`.

The current binaries are unsigned. The Inno Setup compiler and Microsoft runtime bootstrapper are independently signature-verified before use; that does not sign Translate itself.
