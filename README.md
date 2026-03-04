# ⚡ RealiTLScanner GUI

A modern, dark-themed Windows GUI for [RealiTLScanner](https://github.com/XTLS/RealiTLScanner) — a powerful TLS 1.3 scanner designed to find feasible REALITY destinations.

> **Note:** This GUI wraps the original CLI scanner with a graphical interface. All scanning logic remains unchanged.

---

## ✨ Features

- **Three Scan Modes** — Scan by IP / CIDR / Domain, from a file, or crawl domains from a URL
- **All CLI Options** — Port, threads, timeout, output file, verbose mode, IPv6 support
- **Real-Time Log** — Color-coded live output (🟢 feasible, 🔴 errors, 🟣 commands)
- **Results Table** — Sortable DataGrid with columns: IP, Origin, Cert Domain, Cert Issuer, Geo Code
- **Right-Click Copy** — Copy IP, domain, row, or all results to clipboard
- **GeoIP Auto-Download** — Automatically downloads and updates `Country.mmdb` every 7 days
- **CSV Export** — Export scan results to a CSV file
- **Dark Theme** — Premium dark UI with custom scrollbars, styled inputs, and smooth visuals
- **Start / Stop** — Start a scan and stop it at any time
- **Elapsed Timer** — Track how long the scan has been running

---

## 📸 Screenshots

<!-- Add your screenshots below -->

*Screenshots coming soon...*

---

## 📋 Requirements

- **Windows 10/11**
- **.NET 9.0 Runtime** — [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- `RealiTLScanner-windows-64.exe` — already included in the project directory

---

## 🚀 Quick Start

1. Download the latest release or build from source
2. Run `RealiTLScannerGUI.exe` (scanner executable is already included)
3. Enter a target (IP, CIDR, domain) and click **▶ Start Scan**

The GeoIP database (`Country.mmdb`) will be downloaded automatically on first launch.

---

## 🔧 Building from Source

### Framework-dependent (requires .NET Runtime, ~615KB)

```bash
cd RealiTLScannerGUI
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```

### Self-contained (no runtime needed, ~70MB)

```bash
cd RealiTLScannerGUI
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish-standalone
```

---

## 🛠️ CLI Options Reference

The GUI maps all original CLI flags to intuitive controls:

| CLI Flag | GUI Control | Default |
|----------|------------|---------|
| `-addr` | IP / CIDR / Domain text input | — |
| `-in` | File picker (Browse button) | — |
| `-url` | URL text input | — |
| `-port` | Port number field | `443` |
| `-thread` | Threads number field | `2` |
| `-timeout` | Timeout number field | `10` |
| `-out` | Output file path | `out.csv` |
| `-v` | Verbose checkbox | `off` |
| `-46` | Enable IPv6 checkbox | `off` |


---

## 🙏 Acknowledgments

This GUI is built on top of the original **[RealiTLScanner](https://github.com/XTLS/RealiTLScanner)** by its author. Full credit goes to them for the core scanning engine. This project simply adds a graphical frontend to make the tool more accessible.

---

## 📄 License

This project follows the same license as the original [RealiTLScanner](https://github.com/XTLS/RealiTLScanner).
