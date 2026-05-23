using IWshRuntimeLibrary;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;

public class Tools 
{
    // Start DISM/SFC
    public static bool FileChecker() {
        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo {
            UseShellExecute = true,
            FileName = "cmd.exe",
            Arguments = "pause | /k dism /online /cleanup-image /restorehealth&sfc /scannow"
        };
        process.StartInfo = startInfo;
        process.Start();

        return true;
    }

    // Start Updates to third party applications
    public static bool ThirdPartyUpdater() {
        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo {
            UseShellExecute = true,
            FileName = "cmd.exe",
            Arguments = "pause | /k winget upgrade --all"
        };
        process.StartInfo = startInfo;
        process.Start();

        return true;
    }

    // Create Shortcuts
    private static void Shortcut(string shortcutName, string targetFileLocation) {
        // Initialize shortcuts
        string shortcutLocation = Path.Combine(@"C:\Users\Public\Desktop\Nerds On Call 800-919-6373", shortcutName + ".lnk");
        WshShell shell = new WshShell();
        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

        shortcut.TargetPath = targetFileLocation; // The path of the file that will launch when the shortcut is run
        shortcut.Save();
    }

    /*
     * Create shortcut for Calling Card in NOC folder.
     * 
     * If there's a Windows Installer process running "Remote.msi," we will wait until that's finished executing, then we will attempt to
     * create the Calling Card shortcut in the NOC folder.
     * 
     * @author Lukas Lynch
     */
    public static void MakeSupportShortcut() {
        const string dir = @"C:\Users\Public\Desktop\Nerds On Call 800-919-6373";

        // Since there are multiple possible channel IDs, we'll iterate through them. We need to break up the directory and executable since
        // we need to combine them with the ID in the middle.
        const string callingCardDirectory = @"C:\Program Files (x86)\LogMeIn Rescue Calling Card\";
        const string callingCardExecutable = @"\CallingCard.exe";
        string[] channelIds = { "6gqmpb", "eost6i", "58pq3u","ypub4n","1jww6o","gekxnn" };

        // No point in continuing if the NOC Folder isn't present.
        if (!Directory.Exists(dir))
            return;

        // We check for all Windows Installer processes, and if they're running on "Remote.msi" (as indicated by its CommandLine arguments),
        // we wait for it to exit, then break out of the loop and attempt to create the shortcut in the NOC folder.
        foreach (Process process in Process.GetProcessesByName("msiexec")) {
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id)) {
                var managementObject = searcher.Get().Cast<System.Management.ManagementObject>().FirstOrDefault();
                if (managementObject != null && managementObject["CommandLine"]?.ToString().Contains("Remote.msi") == true) {
                    process.WaitForExit();
                    break;
                }
            }
        }

        foreach (string id in channelIds) {
            string path = callingCardDirectory + id + callingCardExecutable;
            if (System.IO.File.Exists(path)) {
                Shortcut("Nerds On Call Support", path);
                break;
            }
        }
    }

    // Make NOC Folder
    public static async Task<bool> MakeNOC(Tool Mb, Tool Mr, Tool Gl) {
        const string dir = @"C:\Users\Public\Desktop\Nerds On Call 800-919-6373";
        const string oldDir = @"C:\Users\Public\Desktop\Nerds On Call 800-919NERD";

        // If directory does not exist, create it
        if (Directory.Exists(oldDir))
            Directory.Move(oldDir, dir);
        else if (!Directory.Exists(dir)) {
            DirectoryInfo folder = Directory.CreateDirectory(dir);

                // Create desktop.ini file
                string deskIni = @"C:\Users\Public\Desktop\Nerds On Call 800-919-6373\desktop.ini";
            using (StreamWriter sw = new StreamWriter(deskIni)) {
                sw.WriteLine("[.ShellClassInfo]");
                sw.WriteLine("ConfirmFileOp=0");
                sw.WriteLine("IconFile=nerd.ico");
                sw.WriteLine("IconIndex=0");
                sw.WriteLine("InfoTip=Contains the Nerds On Call Security Suite");
                sw.Close();
            }

            string place = @"C:\Users\Public\Desktop\Nerds On Call 800-919-6373\nerd.ico";
            StreamResourceInfo sri = Application.GetResourceStream(new Uri("/nerd.ico", UriKind.Relative));

            if (sri != null) {
                using (Stream stream = sri.Stream) {
                    using (var file = System.IO.File.Create(place)) {
                        await stream.CopyToAsync(file);
                    }
                }
            }


            // Copy Nerds icon then set Attributes
            System.IO.File.SetAttributes(place, FileAttributes.Hidden);

            // Hide icon and desktop.ini then set folder as a system folder
            System.IO.File.SetAttributes(deskIni, FileAttributes.Hidden);
            folder.Attributes |= FileAttributes.System;
            folder.Attributes |= FileAttributes.ReadOnly;
            folder.Attributes |= FileAttributes.Directory;
        }

        // Create Shortcutsss

        // MB Shortcut
        if (System.IO.File.Exists(Mb.ToolLocation))
            Shortcut("Malwarebytes", Mb.ToolLocation);

        // CC Shortcut
        if (System.IO.File.Exists(Mr.ToolLocation))
            Shortcut("Macrium Reflect", Mr.ToolLocation);

        // GU Shortcut
        if (System.IO.File.Exists(Gl.ShortcutLocation))
            Shortcut("Glary Utilities", Gl.ShortcutLocation);

        // ADW Shortcut
        if (System.IO.File.Exists(@"C:\AdwCleaner\ADWCleaner.exe"))
            Shortcut("ADWCleaner", @"C:\AdwCleaner\ADWCleaner.exe");
        else if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "ADWCleaner.exe")))
            Shortcut("ADWCleaner", Path.Combine(Directory.GetCurrentDirectory(), "ADWCleaner.exe"));

        // Calling card Shortcut
        MakeSupportShortcut();

        return true;
    }

    // Adds Registry keys so next time Chrome or Edge opens, it updates or asks to install UBlock Origin
    public static async Task<bool> InstallUB() {
        string valueName = "update_url";

        var GC = await Task.Run<bool>(() => {
            // Write to Google Chrome
            string value = "https://clients2.google.com/service/update2/crx";
            string key = @"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Google\Chrome\Extensions\ddkjiahejlhfcafbddmgiahcphecmpfh";
            Registry.SetValue(key, valueName, value, RegistryValueKind.String);
            return true;
        });

        var edge = await Task.Run<bool>(() => {
            /// Write to MS Edge
            string eValue = "https://edge.microsoft.com/extensionwebstorebase/v1/crx";
            string eKey = @"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Microsoft\Edge\Extensions\cimighlppcgcoapaliogpjjdehbnofhn";
            Registry.SetValue(eKey, valueName, eValue, RegistryValueKind.String);
            return true;
        });

        return true;
    }

    /**
     * Installs uBlock Origin for all profiles in Firefox
     * 
     * For the Chrome/Edge version of this function, we just write to the registry and Chrome/Edge
     * will install the extension on next launch, but for Firefox we need to download it ourselves
     * and modify the distribution policies.
     * 
     * @author Lukas Lynch
     */
    public static async Task<bool> InstallUB_Firefox() {
        const string extensionID = "uBlock0@raymondhill.net";
        const string url = "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi";

        // Firefox directory in AppData: %AppData%\Mozilla\Firefox\
        string rootDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mozilla","Firefox"
        );

        // 1.) Fetching data that we'll output into an XPI file(s)
        byte[] xpiData;
        try {
            xpiData = await (new HttpClient()).GetByteArrayAsync(url);
        } catch(HttpRequestException err) {
            Debug.WriteLine($"InstallUB_Firefox/Download: Failed to download uBlock xpi: {err.Message}");
            return false;
        }

        // 2.) Locate each profile directory and install uBO for all of them
        foreach(string profileDir in GetProfileDirs())
            InstallToProfile(profileDir, xpiData);

        // 3.) Update Firefox's distribution policies
        UpdateDistPolicies();

        /* -- Supporting functions -- */

        /**
         * Returns iterable list of paths to all profiles listed in profiles.ini
         */
        IEnumerable<string> GetProfileDirs() {
            if(!System.IO.Directory.Exists(rootDir)) {
                Debug.WriteLine($"InstallUB_Firefox/GetProfileDirs: Firefox profiles root not found: {rootDir}");
                yield return null;
            }

            string iniPath = Path.Combine(rootDir,"profiles.ini");
            if(!System.IO.File.Exists(iniPath)) {
                Debug.WriteLine($"InstallUB_Firefox/GetProfileDirs: profiles.ini not found: {iniPath}");
                yield return null;
            }

            foreach(string line in System.IO.File.ReadAllLines(iniPath)) {
                if(!line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                    continue;

                string rawPath = line.Substring("Path=".Length).Trim().Replace('/', '\\');
                bool isAbsolute = Path.IsPathRooted(rawPath);

                // If the file path contains a drive letter, then we don't need to combine with the Firefox root dir
                string fullPath = isAbsolute ? rawPath : Path.Combine(rootDir, rawPath);
                if(Directory.Exists(fullPath))
                    yield return fullPath;
            }
        } // GetProfileDirs()

        /*
         * For each profile passed, we write the fetched xpiData into profileDir\extensions\uBlock0@raymondhill.net.xpi
         */
        void InstallToProfile(string profileDir, byte[] xpiBytes) {
            if(profileDir == null)
                return;

            string extensionsDir = Path.Combine(profileDir, "extensions");
            Directory.CreateDirectory(extensionsDir);

            string dest = Path.Combine(extensionsDir, $"{extensionID}.xpi");
            System.IO.File.WriteAllBytes(dest, xpiBytes);
        } // InstallToProfile


        /*
         * Updating rootDir\distribution\policies.json so Firefox will act as though uBO was installed
         * by an IT admin or something. tbh I'm not sure why we need to do this lol
         */
        void UpdateDistPolicies() {
            string distDir = Path.Combine(rootDir, "distribution");
            string policyPath = Path.Combine(distDir, "policies.json");
            Directory.CreateDirectory(distDir);

            // If the file already exists we'll parse the pre-existing JSON, or we'll create a new JSON object
            JObject root = System.IO.File.Exists(policyPath)
                ? JObject.Parse(System.IO.File.ReadAllText(policyPath))
                : new JObject();

            /*
             * "policies" {
             *      "ExtensionSettings": {
             *          "uBlock0@raymondhill.net": {
             *              "installation_mode" = "force_installed",
             *              "installation_url" = `url`
             *          }
             *      }
             *  }
             */
            JObject policies = root["policies"] as JObject ?? new JObject();
            JObject extensionSettings = policies["ExtensionSettings"] as JObject ?? new JObject();
            extensionSettings[extensionID] = new JObject {
                ["installation_mode"] = "force_installed",
                ["install_url"] = url
            };

            policies["ExtensionSettings"] = extensionSettings;
            root["policies"] = policies;

            System.IO.File.WriteAllText(policyPath, root.ToString(Formatting.Indented));
        } // UpdateDistPolicies

        return true;
    } // InstallUB_Firefox
}