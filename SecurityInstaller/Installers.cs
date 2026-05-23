using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;

/**
 * For the "Misc. Installers" tab.
 * 
 * We'll populate the tab with checkboxes for every executable found in a local "installers" or "ninite" folder.
 * They can be run by just clicking the 'Start' button. It's independent of the 'Download,'
 * 'Install,' and 'Run' checkboxes.
 */
public static class Installers {

    // We need to associate the checkboxes with the paths to the executables.
    public struct installer {
        public CheckBox box;
        public String path;

        public installer(CheckBox box,String path) {
            this.box = box;
            this.path = path;
        }
    }

    public static List<installer> misc_installers = new List<installer>();

    /**
     * Searches for and in a local folder named 'installers' or 'ninite', and for every executable it finds,
     * it creates a checkbox in the 'Misc. Installers' tab.
     */
    public static void FindMiscInstallers(StackPanel parent) {
        string installersFolder = Path.Combine(Directory.GetCurrentDirectory(),"installers");
        string niniteFolder = Path.Combine(Directory.GetCurrentDirectory(),"ninite");

        if(Directory.Exists(installersFolder))
            foreach(string file in Directory.EnumerateFiles(installersFolder))
                check_for_exec(file);
        if(Directory.Exists(niniteFolder))
            foreach(string file in Directory.EnumerateFiles(niniteFolder))
                check_for_exec(file);

        void check_for_exec(string file) {
            if(file.EndsWith(".exe") || file.EndsWith(".msi") || file.EndsWith(".bat") || file.EndsWith("ps1")) {
                // This is the actual CheckBox XAML element
                CheckBox installerCB = new CheckBox {
                    Content = Path.GetFileNameWithoutExtension(file),
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#E8E9ED"),
                    Margin = (Thickness)new ThicknessConverter().ConvertFromString("20,5,0,5"),
                    FontWeight = FontWeights.SemiBold
                };

                // See 'installer' structure below, which tracks the CheckBox and executable path
                installer installer = new installer(installerCB,file);

                parent.Children.Add(installerCB);
                misc_installers.Add(installer);

            }
        }
    }

    /**
     * This just spawns a new process and attempts to run whatever path is passed to it.
     * If you pass a PS script, it will invoke PS on it.
     */
    public static async Task<bool> RunInstaller(string path, IProgress<string> results) {
        try {
            ProcessStartInfo startInfo;
            if(path.EndsWith(".ps1"))
                startInfo = new ProcessStartInfo {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File {path}"
                };
            else
                startInfo = new ProcessStartInfo {
                    FileName = path
                };
            (new Process { StartInfo = startInfo }).Start();

            results.Report($"Launched: {Path.GetFileName(path)}");

            return true;
        } catch(Exception ex) {
            results.Report($"\nError Opening:{path}\n{ex.Message}");
            return false;
        }
    }
}