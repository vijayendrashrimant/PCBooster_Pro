using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace PCBooster.Services
{
    public enum SecurityLevel { Safe, Warning, Critical }

    public class SecurityItem
    {
        public string        Name        { get; set; }
        public string        Description { get; set; }
        public SecurityLevel Level       { get; set; }
        public string        Action      { get; set; }
        public bool          Fixed       { get; set; }

        public string Icon => Level switch
        {
            SecurityLevel.Safe     => "✅",
            SecurityLevel.Warning  => "⚠",
            SecurityLevel.Critical => "🚨",
            _                      => "❓"
        };

        public string LevelText => Level.ToString();
    }

    public static class SecurityScannerService
    {
        public static List<SecurityItem> RunScan()
        {
            var items = new List<SecurityItem>();

            items.Add(CheckWindowsDefender());
            items.Add(CheckFirewall());
            items.Add(CheckUAC());
            items.Add(CheckWindowsUpdate());
            items.Add(CheckRemoteDesktop());
            items.Add(CheckAutoRun());
            items.Add(CheckGuestAccount());
            items.Add(CheckPasswordPolicy());

            return items;
        }

        // ── Individual checks ────────────────────────────────────────────

        private static SecurityItem CheckWindowsDefender()
        {
            bool enabled = false;
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    @"root\Microsoft\Windows\Defender",
                    "SELECT * FROM MSFT_MpComputerStatus");
                foreach (ManagementObject obj in searcher.Get())
                {
                    enabled = (bool)(obj["AntivirusEnabled"] ?? false);
                    bool rtEnabled = (bool)(obj["RealTimeProtectionEnabled"] ?? false);
                    bool upToDate  = (bool)(obj["AntivirusSignatureOutOfDate"] ?? true) == false;

                    if (!enabled || !rtEnabled)
                        return new SecurityItem
                        {
                            Name        = "Windows Defender",
                            Description = "Real-time protection is OFF.",
                            Level       = SecurityLevel.Critical,
                            Action      = "EnableDefender"
                        };

                    if (!upToDate)
                        return new SecurityItem
                        {
                            Name        = "Defender Definitions",
                            Description = "Virus definitions are out of date.",
                            Level       = SecurityLevel.Warning,
                            Action      = "UpdateDefinitions"
                        };

                    return new SecurityItem
                    {
                        Name        = "Windows Defender",
                        Description = "Real-time protection is active and up to date.",
                        Level       = SecurityLevel.Safe,
                        Action      = ""
                    };
                }
            }
            catch { }

            return new SecurityItem
            {
                Name        = "Windows Defender",
                Description = "Could not read status. Verify manually.",
                Level       = SecurityLevel.Warning,
                Action      = "OpenDefender"
            };
        }

        private static SecurityItem CheckFirewall()
        {
            try
            {
                string output = RunCmd("netsh", "advfirewall show allprofiles state");
                bool allOn = output.Contains("ON") && !output.Contains("OFF");
                return new SecurityItem
                {
                    Name        = "Windows Firewall",
                    Description = allOn ? "All profiles are active." : "One or more firewall profiles are OFF.",
                    Level       = allOn ? SecurityLevel.Safe : SecurityLevel.Critical,
                    Action      = allOn ? "" : "EnableFirewall"
                };
            }
            catch { }
            return new SecurityItem
            {
                Name = "Windows Firewall", Description = "Status unknown.",
                Level = SecurityLevel.Warning, Action = "OpenFirewall"
            };
        }

        private static SecurityItem CheckUAC()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false);
                int val = Convert.ToInt32(key?.GetValue("EnableLUA") ?? 1);
                bool on = val == 1;
                return new SecurityItem
                {
                    Name        = "User Account Control (UAC)",
                    Description = on ? "UAC is enabled — elevation prompts are active."
                                     : "UAC is disabled — system is vulnerable.",
                    Level       = on ? SecurityLevel.Safe : SecurityLevel.Critical,
                    Action      = on ? "" : "EnableUAC"
                };
            }
            catch { }
            return new SecurityItem
            {
                Name = "UAC", Description = "Could not read UAC status.",
                Level = SecurityLevel.Warning, Action = ""
            };
        }

        private static SecurityItem CheckWindowsUpdate()
        {
            // We check if Windows Update service is running
            try
            {
                using var svc = new System.ServiceProcess.ServiceController("wuauserv");
                bool running = svc.Status == System.ServiceProcess.ServiceControllerStatus.Running;
                return new SecurityItem
                {
                    Name        = "Windows Update",
                    Description = running
                        ? "Update service is running."
                        : "Windows Update service is stopped — updates may be blocked.",
                    Level  = running ? SecurityLevel.Safe : SecurityLevel.Warning,
                    Action = running ? "" : "OpenWindowsUpdate"
                };
            }
            catch { }
            return new SecurityItem
            {
                Name = "Windows Update", Description = "Check for updates manually.",
                Level = SecurityLevel.Warning, Action = "OpenWindowsUpdate"
            };
        }

        private static SecurityItem CheckRemoteDesktop()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Terminal Server", false);
                int val = Convert.ToInt32(key?.GetValue("fDenyTSConnections") ?? 1);
                bool rdpEnabled = val == 0;
                return new SecurityItem
                {
                    Name        = "Remote Desktop (RDP)",
                    Description = rdpEnabled
                        ? "RDP is enabled — ensure strong password if needed."
                        : "RDP is disabled — good for home users.",
                    Level       = rdpEnabled ? SecurityLevel.Warning : SecurityLevel.Safe,
                    Action      = rdpEnabled ? "DisableRDP" : ""
                };
            }
            catch { }
            return new SecurityItem
            {
                Name = "Remote Desktop", Description = "Status unknown.",
                Level = SecurityLevel.Warning, Action = ""
            };
        }

        private static SecurityItem CheckAutoRun()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", false);
                int val = Convert.ToInt32(key?.GetValue("NoDriveTypeAutoRun") ?? 0);
                bool disabled = val == 255 || val == 91;
                return new SecurityItem
                {
                    Name        = "AutoRun / AutoPlay",
                    Description = disabled
                        ? "AutoRun is disabled — USB drives won't auto-execute."
                        : "AutoRun may be enabled — can be a malware vector.",
                    Level       = disabled ? SecurityLevel.Safe : SecurityLevel.Warning,
                    Action      = disabled ? "" : "DisableAutoRun"
                };
            }
            catch { }
            return new SecurityItem
            {
                Name = "AutoRun", Description = "Consider disabling AutoRun for USB drives.",
                Level = SecurityLevel.Warning, Action = "DisableAutoRun"
            };
        }

        private static SecurityItem CheckGuestAccount()
        {
            try
            {
                string output = RunCmd("net", "user Guest");
                bool active = output.Contains("Account active") && output.Contains("Yes");
                return new SecurityItem
                {
                    Name        = "Guest Account",
                    Description = active ? "Guest account is enabled — potential security risk."
                                         : "Guest account is disabled.",
                    Level       = active ? SecurityLevel.Warning : SecurityLevel.Safe,
                    Action      = active ? "DisableGuest" : ""
                };
            }
            catch { }
            return new SecurityItem
            {
                Name = "Guest Account", Description = "Status unavailable.",
                Level = SecurityLevel.Warning, Action = ""
            };
        }

        private static SecurityItem CheckPasswordPolicy()
        {
            try
            {
                string output = RunCmd("net", "accounts");
                bool hasExpiry = !output.Contains("Never");
                return new SecurityItem
                {
                    Name        = "Password Policy",
                    Description = hasExpiry
                        ? "Password expiration policy is active."
                        : "Passwords never expire — consider setting an expiry.",
                    Level       = hasExpiry ? SecurityLevel.Safe : SecurityLevel.Warning,
                    Action      = ""
                };
            }
            catch { }
            return new SecurityItem
            {
                Name = "Password Policy", Description = "Check manually via secpol.msc.",
                Level = SecurityLevel.Warning, Action = ""
            };
        }

        // ── Fix actions ──────────────────────────────────────────────────

        public static void ExecuteAction(string action)
        {
            switch (action)
            {
                case "EnableDefender":
                case "OpenDefender":
                    Process.Start(new ProcessStartInfo(
                        "windowsdefender://") { UseShellExecute = true });
                    break;
                case "UpdateDefinitions":
                    RunCmd("powershell", "-Command \"Update-MpSignature\"");
                    break;
                case "EnableFirewall":
                    RunCmd("netsh", "advfirewall set allprofiles state on");
                    break;
                case "OpenFirewall":
                    Process.Start(new ProcessStartInfo(
                        "ms-settings:windowsdefender") { UseShellExecute = true });
                    break;
                case "OpenWindowsUpdate":
                    Process.Start(new ProcessStartInfo(
                        "ms-settings:windowsupdate") { UseShellExecute = true });
                    break;
                case "DisableRDP":
                    RunCmd("reg", @"add ""HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server"" /v fDenyTSConnections /t REG_DWORD /d 1 /f");
                    break;
                case "DisableAutoRun":
                    RunCmd("reg", @"add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f");
                    break;
                case "DisableGuest":
                    RunCmd("net", "user Guest /active:no");
                    break;
            }
        }

        public static int GetScore(List<SecurityItem> items)
        {
            int score = 100;
            foreach (var item in items)
            {
                if (item.Level == SecurityLevel.Critical) score -= 20;
                else if (item.Level == SecurityLevel.Warning) score -= 8;
            }
            return Math.Max(0, score);
        }

        private static string RunCmd(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(5000);
            return output;
        }
    }
}
