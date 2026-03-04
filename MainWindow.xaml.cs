using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace RealiTLScannerGUI
{
    public class ScanResult
    {
        public string IP { get; set; } = "";
        public string Origin { get; set; } = "";
        public string CertDomain { get; set; } = "";
        public string CertIssuer { get; set; } = "";
        public string GeoCode { get; set; } = "";
    }

    public partial class MainWindow : Window
    {
        private Process? _scanProcess;
        private bool _isScanning;
        private readonly ObservableCollection<ScanResult> _results = new();
        private readonly StringBuilder _logBuilder = new();
        private DateTime _startTime;
        private DispatcherTimer? _elapsedTimer;
        private string _exePath = "";

        private const string GeoDbUrl = "https://github.com/Loyalsoldier/geoip/releases/latest/download/Country.mmdb";
        private const string GeoDbFileName = "Country.mmdb";
        private const int GeoUpdateIntervalDays = 7;
        private static readonly HttpClient _httpClient = new();

        public MainWindow()
        {
            InitializeComponent();
            ResultsGrid.ItemsSource = _results;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Try to find the scanner exe relative to this app's directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Look in parent directories (since GUI is in subfolder)
            var candidates = new[]
            {
                Path.Combine(appDir, "RealiTLScanner-windows-64.exe"),
                Path.Combine(appDir, "..", "RealiTLScanner-windows-64.exe"),
                Path.Combine(appDir, "..", "..", "RealiTLScanner-windows-64.exe"),
                Path.Combine(appDir, "..", "..", "..", "RealiTLScanner-windows-64.exe"),
                Path.Combine(appDir, "..", "..", "..", "..", "RealiTLScanner-windows-64.exe"),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    _exePath = fullPath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(_exePath))
            {
                AppendLog("[WARN] RealiTLScanner-windows-64.exe not found! Please place it next to this application.\n", isWarning: true);
            }
            else
            {
                AppendLog($"[INFO] Scanner found: {_exePath}\n");
            }

            UpdatePlaceholder();

            // Auto-download/update GeoIP database
            _ = CheckAndDownloadGeoDbAsync();
        }

        private async Task CheckAndDownloadGeoDbAsync()
        {
            try
            {
                string scannerDir = !string.IsNullOrEmpty(_exePath)
                    ? Path.GetDirectoryName(_exePath) ?? AppDomain.CurrentDomain.BaseDirectory
                    : AppDomain.CurrentDomain.BaseDirectory;

                string geoDbPath = Path.Combine(scannerDir, GeoDbFileName);

                bool needsDownload = false;
                if (!File.Exists(geoDbPath))
                {
                    AppendLog($"[GEO] Country.mmdb not found. Downloading...\n", isWarning: true);
                    needsDownload = true;
                }
                else
                {
                    var fileAge = DateTime.Now - File.GetLastWriteTime(geoDbPath);
                    if (fileAge.TotalDays > GeoUpdateIntervalDays)
                    {
                        AppendLog($"[GEO] Country.mmdb is {fileAge.Days} days old. Updating...\n", isWarning: true);
                        needsDownload = true;
                    }
                    else
                    {
                        AppendLog($"[GEO] Country.mmdb is up to date ({fileAge.Days}d old)\n");
                    }
                }

                if (needsDownload)
                {
                    SetStatus("Downloading GeoIP database...", Brushes.Orange);
                    BtnStart.IsEnabled = false;

                    string tempPath = geoDbPath + ".tmp";

                    await Task.Run(async () =>
                    {
                        using var response = await _httpClient.GetAsync(GeoDbUrl, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        var totalBytes = response.Content.Headers.ContentLength ?? -1;

                        using var contentStream = await response.Content.ReadAsStreamAsync();
                        using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                        var buffer = new byte[8192];
                        long downloaded = 0;
                        int bytesRead;
                        DateTime lastUpdate = DateTime.MinValue;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloaded += bytesRead;

                            if ((DateTime.Now - lastUpdate).TotalMilliseconds > 500)
                            {
                                lastUpdate = DateTime.Now;
                                var mb = downloaded / 1024.0 / 1024.0;
                                var totalMb = totalBytes > 0 ? totalBytes / 1024.0 / 1024.0 : -1;
                                var progressText = totalMb > 0
                                    ? $"[GEO] Downloading: {mb:F1} / {totalMb:F1} MB ({(double)downloaded / totalBytes * 100:F0}%)\r"
                                    : $"[GEO] Downloading: {mb:F1} MB...\r";

                                await Dispatcher.InvokeAsync(() => SetStatus(
                                    totalMb > 0
                                        ? $"Downloading GeoIP database... {mb:F1}/{totalMb:F1} MB"
                                        : $"Downloading GeoIP database... {mb:F1} MB",
                                    Brushes.Orange));
                            }
                        }
                    });

                    // Replace old file with new one
                    if (File.Exists(geoDbPath))
                        File.Delete(geoDbPath);
                    File.Move(tempPath, geoDbPath);

                    var sizeMb = new FileInfo(geoDbPath).Length / 1024.0 / 1024.0;
                    AppendLog($"[GEO] ✓ Country.mmdb downloaded successfully ({sizeMb:F1} MB)\n", isFeasible: true);

                    SetStatus("Ready", new SolidColorBrush((Color)FindResource("TextSecondary")));
                    BtnStart.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[GEO] ⚠ Failed to download Country.mmdb: {ex.Message}\n", isError: true);
                AppendLog($"[GEO] You can download it manually from:\n", isWarning: true);
                AppendLog($"[GEO] {GeoDbUrl}\n", isWarning: true);
                SetStatus("Ready (GeoIP unavailable)", new SolidColorBrush((Color)FindResource("AccentOrange")));
                BtnStart.IsEnabled = true;
            }
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            if (TxtTarget == null || BtnBrowse == null) return;

            if (ModeAddr.IsChecked == true)
            {
                TxtTarget.Tag = "Enter IP, CIDR, or domain (e.g. 1.1.1.1, 10.0.0.0/24, example.com)";
                TxtTarget.Text = "";
                BtnBrowse.Visibility = Visibility.Collapsed;
                TxtTarget.IsReadOnly = false;
            }
            else if (ModeFile.IsChecked == true)
            {
                TxtTarget.Tag = "Select an input file with IPs/CIDRs/domains...";
                TxtTarget.Text = "";
                BtnBrowse.Visibility = Visibility.Visible;
                TxtTarget.IsReadOnly = true;
            }
            else if (ModeUrl.IsChecked == true)
            {
                TxtTarget.Tag = "Enter URL to crawl domains from (e.g. https://launchpad.net/ubuntu/+archivemirrors)";
                TxtTarget.Text = "";
                BtnBrowse.Visibility = Visibility.Collapsed;
                TxtTarget.IsReadOnly = false;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select input file",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                TxtTarget.Text = dlg.FileName;
            }
        }

        private void BtnOutputBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Select output file",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = TxtOutput.Text
            };
            if (dlg.ShowDialog() == true)
            {
                TxtOutput.Text = dlg.FileName;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning) return;

            if (string.IsNullOrEmpty(_exePath))
            {
                MessageBox.Show(
                    "RealiTLScanner-windows-64.exe not found!\n\nPlease place the scanner executable next to this application or in the parent directory.",
                    "Scanner Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var target = TxtTarget.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("Please specify a target to scan.", "Missing Target",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Build arguments
            var args = new StringBuilder();

            if (ModeAddr.IsChecked == true)
                args.Append($"-addr \"{target}\" ");
            else if (ModeFile.IsChecked == true)
                args.Append($"-in \"{target}\" ");
            else if (ModeUrl.IsChecked == true)
                args.Append($"-url \"{target}\" ");

            if (int.TryParse(TxtPort.Text, out var port) && port != 443)
                args.Append($"-port {port} ");

            if (int.TryParse(TxtThreads.Text, out var threads) && threads != 2)
                args.Append($"-thread {threads} ");

            if (int.TryParse(TxtTimeout.Text, out var timeout) && timeout != 10)
                args.Append($"-timeout {timeout} ");

            var outFile = TxtOutput.Text.Trim();
            if (!string.IsNullOrEmpty(outFile) && outFile != "out.csv")
                args.Append($"-out \"{outFile}\" ");

            if (ChkVerbose.IsChecked == true)
                args.Append("-v ");

            if (ChkIpv6.IsChecked == true)
                args.Append("-46 ");

            StartScan(args.ToString().Trim());
        }

        private void StartScan(string arguments)
        {
            _logBuilder.Clear();
            TxtLog.Text = "";
            TxtLog.Inlines.Clear();
            _results.Clear();
            UpdateResultCount();

            _isScanning = true;
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            SetStatus("Scanning...", Brushes.Orange);

            AppendLog($"[CMD] RealiTLScanner-windows-64.exe {arguments}\n", isCommand: true);
            AppendLog($"[INFO] Starting scan...\n");

            _startTime = DateTime.Now;
            StartElapsedTimer();

            var startInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_exePath) ?? "",
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            try
            {
                _scanProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                _scanProcess.OutputDataReceived += Process_OutputDataReceived;
                _scanProcess.ErrorDataReceived += Process_ErrorDataReceived;
                _scanProcess.Exited += Process_Exited;
                _scanProcess.Start();
                _scanProcess.BeginOutputReadLine();
                _scanProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Failed to start scanner: {ex.Message}\n", isError: true);
                ScanFinished();
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            Dispatcher.BeginInvoke(() =>
            {
                var line = e.Data;
                bool isFeasible = line.Contains("feasible=true");
                AppendLog(line + "\n", isFeasible: isFeasible);

                // Try to parse feasible results from log lines
                if (isFeasible)
                {
                    TryParseResult(line);
                }
            });
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            Dispatcher.BeginInvoke(() =>
            {
                AppendLog(e.Data + "\n", isError: true);
            });
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                AppendLog("\n[INFO] Scan process exited.\n");
                ScanFinished();

                // Try to load results from the output CSV file
                LoadResultsFromCsv();
            });
        }

        private void LoadResultsFromCsv()
        {
            var outFile = TxtOutput.Text.Trim();
            if (string.IsNullOrEmpty(outFile)) outFile = "out.csv";

            // Resolve relative to scanner's directory
            string csvPath;
            if (Path.IsPathFullyQualified(outFile))
            {
                csvPath = outFile;
            }
            else
            {
                csvPath = Path.Combine(Path.GetDirectoryName(_exePath) ?? "", outFile);
            }

            if (!File.Exists(csvPath))
            {
                AppendLog($"[INFO] Output file not found at: {csvPath}\n");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(csvPath);
                // Skip header
                int loadedCount = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    var parsed = ParseCsvLine(lines[i]);
                    if (parsed != null)
                    {
                        // Avoid duplicates
                        bool exists = false;
                        foreach (var r in _results)
                        {
                            if (r.IP == parsed.IP && r.CertDomain == parsed.CertDomain)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            _results.Add(parsed);
                            loadedCount++;
                        }
                    }
                }

                if (loadedCount > 0)
                {
                    AppendLog($"[INFO] Loaded {loadedCount} results from {csvPath}\n");
                }

                UpdateResultCount();
            }
            catch (Exception ex)
            {
                AppendLog($"[WARN] Could not read CSV: {ex.Message}\n", isWarning: true);
            }
        }

        private ScanResult? ParseCsvLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            // CSV with possible quoted fields: IP,ORIGIN,CERT_DOMAIN,CERT_ISSUER,GEO_CODE
            var parts = SplitCsvLine(line);
            if (parts.Count < 5) return null;

            return new ScanResult
            {
                IP = parts[0],
                Origin = parts[1],
                CertDomain = parts[2],
                CertIssuer = parts[3].Trim('"'),
                GeoCode = parts[4]
            };
        }

        private System.Collections.Generic.List<string> SplitCsvLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }

        private void TryParseResult(string logLine)
        {
            // Parse structured log output like:
            // time=... level=INFO msg="Connected to target" feasible=true ip=X origin=Y ...
            try
            {
                string ip = ExtractField(logLine, "ip=");
                string origin = ExtractField(logLine, "origin=");
                string certDomain = ExtractField(logLine, "cert-domain=");
                string certIssuer = ExtractField(logLine, "cert-issuer=");
                string geo = ExtractField(logLine, "geo=");

                if (!string.IsNullOrEmpty(ip))
                {
                    _results.Add(new ScanResult
                    {
                        IP = ip,
                        Origin = origin,
                        CertDomain = certDomain,
                        CertIssuer = certIssuer,
                        GeoCode = geo
                    });
                    UpdateResultCount();
                }
            }
            catch
            {
                // Parsing failed, ignore
            }
        }

        private string ExtractField(string text, string key)
        {
            int idx = text.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return "";

            int start = idx + key.Length;
            if (start >= text.Length) return "";

            // Check if value is quoted
            if (text[start] == '"')
            {
                int end = text.IndexOf('"', start + 1);
                return end > start ? text.Substring(start + 1, end - start - 1) : "";
            }
            else
            {
                int end = text.IndexOf(' ', start);
                return end > start ? text.Substring(start, end - start) : text.Substring(start);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopScan();
        }

        private void StopScan()
        {
            if (_scanProcess != null && !_scanProcess.HasExited)
            {
                try
                {
                    _scanProcess.Kill(entireProcessTree: true);
                    AppendLog("[INFO] Scan stopped by user.\n", isWarning: true);
                }
                catch (Exception ex)
                {
                    AppendLog($"[ERROR] Failed to stop: {ex.Message}\n", isError: true);
                }
            }
            ScanFinished();
        }

        private void ScanFinished()
        {
            _isScanning = false;
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            StopElapsedTimer();

            if (_results.Count > 0)
            {
                SetStatus($"Completed — {_results.Count} result(s) found", 
                    new SolidColorBrush((Color)FindResource("AccentGreen")));
            }
            else
            {
                SetStatus("Completed — no feasible results", 
                    new SolidColorBrush((Color)FindResource("TextSecondary")));
            }
        }

        private void ResultTab_Changed(object sender, RoutedEventArgs e)
        {
            if (LogPanel == null || ResultsPanel == null) return;

            if (TabLog.IsChecked == true)
            {
                LogPanel.Visibility = Visibility.Visible;
                ResultsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                LogPanel.Visibility = Visibility.Collapsed;
                ResultsPanel.Visibility = Visibility.Visible;
            }
        }

        private void AppendLog(string text, bool isError = false, bool isWarning = false, 
            bool isFeasible = false, bool isCommand = false)
        {
            _logBuilder.Append(text);

            var run = new System.Windows.Documents.Run(text);
            if (isError)
                run.Foreground = new SolidColorBrush((Color)FindResource("AccentRed"));
            else if (isWarning)
                run.Foreground = new SolidColorBrush((Color)FindResource("AccentOrange"));
            else if (isFeasible)
                run.Foreground = new SolidColorBrush((Color)FindResource("AccentGreen"));
            else if (isCommand)
                run.Foreground = new SolidColorBrush((Color)FindResource("AccentPurple"));
            else
                run.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");

            TxtLog.Inlines.Add(run);

            // Auto-scroll
            LogScrollViewer.ScrollToBottom();
        }

        private void SetStatus(string text, Brush dotColor)
        {
            TxtStatus.Text = text;
            StatusDot.Fill = dotColor;
        }

        private void UpdateResultCount()
        {
            TxtResultCount.Text = _results.Count > 0 ? $"{_results.Count} result(s)" : "";
        }

        private void StartElapsedTimer()
        {
            _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _elapsedTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _startTime;
                TxtElapsed.Text = $"⏱ {elapsed:hh\\:mm\\:ss}";
            };
            _elapsedTimer.Start();
        }

        private void StopElapsedTimer()
        {
            _elapsedTimer?.Stop();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("No results to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Export results as CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = "scan_results.csv"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("IP,ORIGIN,CERT_DOMAIN,CERT_ISSUER,GEO_CODE");
                    foreach (var r in _results)
                    {
                        sb.AppendLine($"{r.IP},{r.Origin},{r.CertDomain},\"{r.CertIssuer}\",{r.GeoCode}");
                    }
                    File.WriteAllText(dlg.FileName, sb.ToString());
                    MessageBox.Show($"Exported {_results.Count} results to:\n{dlg.FileName}", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isScanning)
            {
                var result = MessageBox.Show(
                    "A scan is still running. Do you want to stop it and close?",
                    "Scan in Progress",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                StopScan();
            }
            base.OnClosing(e);
        }
        private void CopyIP_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is ScanResult result)
            {
                Clipboard.SetText(result.IP);
            }
        }

        private void CopyDomain_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is ScanResult result)
            {
                Clipboard.SetText(result.CertDomain);
            }
        }

        private void CopyRow_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItems.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var item in ResultsGrid.SelectedItems)
                {
                    if (item is ScanResult r)
                    {
                        sb.AppendLine($"{r.IP}\t{r.Origin}\t{r.CertDomain}\t{r.CertIssuer}\t{r.GeoCode}");
                    }
                }
                Clipboard.SetText(sb.ToString().TrimEnd());
            }
        }

        private void CopyAll_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("IP\tORIGIN\tCERT_DOMAIN\tCERT_ISSUER\tGEO_CODE");
            foreach (var r in _results)
            {
                sb.AppendLine($"{r.IP}\t{r.Origin}\t{r.CertDomain}\t{r.CertIssuer}\t{r.GeoCode}");
            }
            Clipboard.SetText(sb.ToString().TrimEnd());
        }
    }
}
