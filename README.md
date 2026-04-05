# EventScanner

**System health and security scanner with forensic event log analysis.**

Grade your Windows system from F to SSS+ by analyzing Event Viewer logs, detecting patterns, and explaining findings in plain language.

> Built with WPF, .NET 9, and Fluent Design. Open source. No paid dependencies.

---

## ✨ Features

- **System grading** — scores your system 0–100 and assigns a grade from F (worst) to SSS+ (best)
- **18 detection rules** across System, Application, Security, and PowerShell logs
- **Forensic .evtx import** — analyze exported event logs from any Windows machine
- **CTF speed import** — same rules and grading as a normal import, but findings are re-sorted for fast triage (severity → how often → most recent activity)
- **CTF pivots (heuristic)** — after a CTF-speed run, selecting a finding surfaces auto-detected IPv4s, paths, `DOMAIN\user`-style accounts, emails, and computer names from raw event text (hints only; verify in context)
- **Finding detail view** — click any finding to see a full explanation, possible causes, and event timeline
- **Animated grade visuals** — glowing, pulsing grade display inspired by Tetris Effect; higher grades earn more impressive effects
- **Ambient background** — very soft neon drift (butterflies / unicorn) plus optional pastel “kawaii” sparkles, hearts, clouds, and blobs; kept low-contrast so text stays readable
- **Severity-colored badges** — Critical (red), High (orange), Medium (amber), Low (teal), Informational (gray)
- **HTML report export** — self-contained dark-themed report; CTF-speed imports include the same heuristic pivot list per finding in the HTML
- **Plain-language explanations** — every finding tells you what happened, why it matters, and what might be causing it
- **Portable .exe** — single file, no installer, no .NET install required

---

## 🚀 Quick Start

### Option A: Run the portable .exe

1. Download `EventScanner.exe` from the latest release
2. Double-click to run — no installation needed
3. Click **Quick Scan** to analyze your system

### Option B: Build from source

```powershell
git clone <your-clone-url>
cd <repo-root>   # folder that contains EventScanner.sln
dotnet build EventScanner.sln
dotnet run --project src/EventScanner/EventScanner.csproj
```

**Requirements:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) and Windows 10/11 (64-bit)

---

## 🛠 Tech Stack

| Component | Technology | Why |
|---|---|---|
| Language | C# / .NET 9 | Best tooling for Windows desktop apps |
| UI Framework | WPF + [WPF-UI 4.2](https://github.com/lepoco/wpfui) | Mature framework with modern Fluent Design controls |
| Architecture | MVVM via [CommunityToolkit.Mvvm 8.4](https://github.com/CommunityToolkit/dotnet) | Clean separation of UI and logic |
| Database | [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/) | Lightweight local storage (ready for future use) |
| Testing | xUnit 2.9 | Most popular .NET test framework |
| Event Logs | System.Diagnostics.Eventing.Reader | Built-in .NET API for Windows Event Viewer |

All dependencies are free and open source.

---

## 🗂 Project Structure

```
EventScanner/
├── src/EventScanner/               # Main application
│   ├── Models/                     # Data shapes (Finding, Grade, ScanResult, etc.)
│   ├── ViewModels/                 # UI logic (MVVM pattern)
│   ├── Services/                   # Log reader, analyzer, grading, scans, HTML export, entity extraction
│   ├── Rules/                      # 18 detection rules (one file per rule)
│   ├── Helpers/                    # Value converters and small utilities
│   ├── Animations/                 # Background animators (neon + kawaii ambient)
│   ├── App.xaml                    # Application entry + WPF-UI theme
│   ├── MainWindow.xaml             # Main dashboard UI
│   └── EventScanner.csproj
├── tests/EventScanner.Tests/       # Automated tests (see Commands section)
│   ├── Models/
│   ├── Services/
│   └── Rules/
├── EventScanner.sln
└── README.md
```

---

## 🔍 Detection Rules (18)

### System Log

| Rule | Event IDs | What it detects |
|---|---|---|
| Unexpected Shutdown | 6008, 41 | System crashes, power loss, forced shutdowns |
| Disk Errors | 7, 11, 15, 51 | Hard drive read/write failures |
| Service Crashes | 7031, 7034 | Windows services that stopped unexpectedly |
| NTFS Errors | 55, 98, 137 | File system corruption |
| Windows Update Failures | 20, 25 | Failed update installations |
| Blue Screen Crashes | 1001 | BSOD / BugCheck events |
| Time Service Errors | 129, 134 | Clock synchronization failures |
| New Service Installed | 7045 | New Windows service registrations |

### Application Log

| Rule | Event IDs | What it detects |
|---|---|---|
| Application Crashes | 1000 | Programs that crashed |
| Application Hangs | 1002 | Programs that stopped responding |

### Security Log

| Rule | Event IDs | What it detects |
|---|---|---|
| Failed Logon Attempts | 4625 | Wrong password / unauthorized access attempts |
| Audit Log Cleared | 1102 | Security log wiped (forensics red flag) |
| User Account Changes | 4720, 4726 | Accounts created or deleted |
| Account Lockouts | 4740 | Accounts locked from too many bad passwords |
| Group Membership Changes | 4728, 4732, 4756 | Users added to security groups |
| Audit Policy Changes | 4719 | Security logging settings modified |
| Scheduled Task Changes | 4698, 4699 | Scheduled tasks created or deleted |

### Windows PowerShell Log

| Rule | Event IDs | What it detects |
|---|---|---|
| PowerShell Activity | 400 | PowerShell engine executions |

---

## 📊 Grading System

The system starts at 100 points and deducts based on findings:

| Severity | Points deducted | Confidence multiplier |
|---|---|---|
| Critical | 30 | High: 1.0x, Medium: 0.7x, Low: 0.4x |
| High | 15 | |
| Medium | 8 | |
| Low | 3 | |
| Informational | 1 | |

| Grade | Score Range | Meaning |
|---|---|---|
| F | 0–19 | Serious issues — immediate attention needed |
| D | 20–34 | Many problems detected |
| C | 35–49 | Below average — notable issues |
| B | 50–64 | Decent — some issues to address |
| A | 65–79 | Good system health |
| S | 80–87 | Excellent — well maintained |
| SS | 88–93 | Outstanding — very clean |
| SSS | 94–98 | Near-perfect |
| SSS+ | 99–100 | Virtually flawless |

---

## ▶️ Usage

### Live System Scan

- **Quick Scan** — analyzes the last 24 hours of event logs
- **Deep Scan** — analyzes the last 30 days of event logs

### Forensic Import

- **Import Logs** — select one or more `.evtx` files exported from any Windows machine
- **CTF speed** — same analysis pipeline and grade; findings ordered for quick manual review and optional heuristic pivots in the detail panel
- The imported logs receive their own separate grade
- Useful for incident response: export logs from a suspect machine, analyze on a clean one

### Export

- **Export Report** — saves a self-contained HTML report (dark theme, grade styling, findings, timelines). For CTF-speed imports, the HTML also includes the heuristic “CTF pivots” block per finding.

### Finding Details

- Click any finding in the list to see:
  - Full explanation in plain language
  - Why it matters
  - Possible causes
  - Sortable event timeline with every individual event
  - **CTF pivots** (only after a CTF-speed import) — parsed hints from raw messages, same rules as in the HTML report

---

## ⌨️ Commands

```powershell
# Build
dotnet build EventScanner.sln

# Run
dotnet run --project src/EventScanner/EventScanner.csproj

# Test (259 tests in the current main-branch suite)
dotnet test EventScanner.sln

# Publish portable .exe
dotnet publish src/EventScanner/EventScanner.csproj -c Release
# Output: src/EventScanner/bin/Release/net9.0-windows/win-x64/publish/EventScanner.exe
```

---

## 🧪 Testing

259 automated tests covering:

- **Model tests** — grade levels, score mapping, finding construction, scan results
- **Rule tests** — every detection rule tested with controlled fake event data
- **Service tests** — event analyzer, grading engine, scan orchestration, file import
- **Integration tests** — reads real event logs from the running machine

```powershell
dotnet test EventScanner.sln --verbosity normal
```

---

## ⚠️ Known Limitations

- **Security log requires admin** — the Security and some PowerShell events need administrator privileges to read. Run as admin for full coverage, or the app gracefully skips inaccessible logs.
- **Windows only** — uses WPF and Windows Event Log APIs. Not cross-platform.
- **No CVE correlation yet** — planned for a future version. Currently detects issues but does not link to specific CVEs.
- **No persistent storage yet** — scan results are not saved between sessions. SQLite is included as a dependency but not wired up yet.
- **No auto-update** — the app does not check for or install updates automatically.
- **Detection rules are pattern-based** — rules use known Event IDs and providers. They may not catch every possible issue.
- **CTF pivots are heuristic** — extracted strings come from unstructured message text; they can miss real indicators or occasionally misparse. Always confirm against the event timeline.
- **Emoji / background visuals** — decorative elements depend on your system’s emoji fonts; if something looks like a box, it is a font limitation, not missing logic.
- **64-bit Windows only** — the portable .exe targets win-x64.

---

## 🛣 Roadmap

- [ ] CVE/advisory correlation from NIST NVD and Microsoft Security Response Center
- [ ] Safe repair actions with risk labeling and undo
- [ ] SQLite storage for scan history and trend tracking
- [ ] Additional detection rules (RDP activity, firewall changes, Sysmon events)
- [ ] Custom rule authoring
- [ ] Grade history and trend charts
- [ ] App auto-updater
- [ ] Custom app icon

---

## 🤝 Contributing

Contributions are welcome. To add a new detection rule:

1. Create a new file in `src/EventScanner/Rules/`
2. Implement the `IRule` interface
3. Register it in `EventAnalyzer.CreateWithDefaultRules()`
4. Add tests in `tests/EventScanner.Tests/Rules/`
5. Only use real, documented Windows Event IDs

---

## 📄 License

[MIT](LICENSE) — free to use, modify, and distribute; see the license file for the full text.

The application also depends on open-source libraries (WPF-UI, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, xUnit, etc.). Those projects have their own licenses.
