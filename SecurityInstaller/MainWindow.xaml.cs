using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using static RoundCorners;

namespace SecurityInstaller {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            try {
                // Call Windows 11 API to Round Corners
                IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
                var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
                var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));
            } catch {
            }
        }

        // When window is loaded
        private async void OnLoad(object sender, RoutedEventArgs e) {
            // Start Global Timer with event
            var timer = new DispatcherTimer();
            // Change frame tick
            int tickRate = 50;
            timer.Interval = TimeSpan.FromMilliseconds(tickRate);
            // Update animations
            timer.Tick += timer_Tick;
            timer.Start();

            DisplayHardwareInfo();
            Installers.FindMiscInstallers(ninite);
        }

        private async void DisplayHardwareInfo() {
            name.Text = await Task<string>.Run(() => $"Device: {Environment.MachineName}\nUser: {Environment.UserName}");
            serialNum.Text = await Task<string>.Run(() => $"{ComputerInfo.BiosInfo[1]}");
            makeModel.Text = await Task<string>.Run(() => $"{ComputerInfo.BoardInfo[0]}\n{ComputerInfo.ProductName}");
            osInfo.Text = await Task<string>.Run(() => $"{ComputerInfo.OsInfo}");
            bios.Text = await Task<string>.Run(() => $"{ComputerInfo.BiosInfo[0]}\n{ComputerInfo.BoardInfo[1]}\nVer: {ComputerInfo.BiosInfo[2]}");
            storage.Text = await Task<string>.Run(() => $"Partitions:\n{ComputerInfo.Partitions}\nLocal / Physical Drives:\n{ComputerInfo.Drives}");
            gpu.Text = await Task<string>.Run(() => $"{ComputerInfo.GPU}");
            memory.Text = await Task<string>.Run(() => $"{ComputerInfo.MaxMemory}\nSpeed: {ComputerInfo.RamSpeed}");
            cpu.Text = await Task<string>.Run(() => $"{ComputerInfo.CPU}");
            mac.Text = await Task<string>.Run(() => $"{ComputerInfo.Mac}");
        }

        private List<Task<bool>> tasks;
        private ToolResource resources;
        private ProgressReportModel report;
        private Progress<ProgressReportModel> progressModel;
        private IProgress<ProgressReportModel> progress => progressModel;
        private IProgress<string> resultsProgress;
        private IProgress<int> progressBarProgress;

        /// <summary>
        /// Main Funtion on Button Click
        /// </summary>
        private async void StartBtn_Click(object sender, EventArgs e) {
            StartBtn.IsEnabled = false;

            // Reset
            ProgressBar1.Value = 0;
            ProgressBar2.Value = 0;

            resources = new ToolResource();

            // Initialize list for tasks for counting / parallel async
            tasks = new List<Task<bool>>();

            // Initialize progress report model
            report = new ProgressReportModel();

            // Initialize custom Progress model and event
            progressModel = new Progress<ProgressReportModel>();
            progressModel.ProgressChanged += ReportProgress;

            // Progress reporter for task completion / error messages
            resultsProgress = new Progress<string>(str => {
                ScriptOutput.Text += $"\n{str}";
            });

            // Progress reporter for how many tasks have completed
            progressBarProgress = new Progress<int>(value => {
                ProgressBar2.Value += value;
                report.DownloadCompleted++;
                progress.Report(report);
            });

            // Add tasks to previous list with progress reporters
            AddTasks();

            // Set botttom progress bar to amount of tasks to-do
            int count = tasks.Count;
            ProgressBar2.Maximum = count;

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // If folder is checked make folder
            if (noc.IsChecked == true) {
                try {
                    await Tools.MakeNOC(resources.Malwarebytes, resources.Macrium, resources.Glary);
                    ScriptOutput.AppendText("\n\nNOC Folder Created");
                    ProgressBar2.Value += 1;
                } catch (Exception ex) {
                    ScriptOutput.AppendText($"\n\nError making NOC Folder. Reason:\n{ex.Message}");
                }
            }

            // Cleanup
            // Delete MB shortcut from installer
            if (File.Exists(@"C:\Users\Public\Desktop\Malwarebytes.lnk"))
                File.Delete(@"C:\Users\Public\Desktop\Malwarebytes.lnk");

            // Delete GU shortcut from installer
            if (File.Exists(@"C:\Users\Public\Desktop\Glary Utilities.lnk"))
                File.Delete(@"C:\Users\Public\Desktop\Glary Utilities.lnk");

            StartBtn.IsEnabled = true;
            ScriptOutput.AppendText("\n\nDone!");
        }

        // Add tasks to a list
        private void AddTasks() {
            if (remote.IsChecked == true) AddProgressTask(resources.Support);
            if (adw.IsChecked == true) AddProgressTask(resources.Adw);
            if (mb.IsChecked == true) AddProgressTask(resources.Malwarebytes);
            if (gu.IsChecked == true) AddProgressTask(resources.Glary);
            if (reflect.IsChecked == true) AddProgressTask(resources.Macrium);

            // if checked
            if (sfc.IsChecked == true) {
                // add task
                tasks.Add(Task.Run(() => Tools.FileChecker()));

                // report results
                resultsProgress.Report("\nDism/SFC Started");

                // report progress
                ProgressBar2.Value += 1;
            }

            if (uBlock.IsChecked == true) {
                tasks.Add(Tools.InstallUB());
                resultsProgress.Report("\nuBlock Origin Lite added to Edge and Chrome");
                ProgressBar2.Value += 1;
            }

            if (uBlock_FF.IsChecked == true) {
                tasks.Add(Tools.InstallUB_Firefox());
                resultsProgress.Report("\nuBlock Origin added to Firefox");
                ProgressBar2.Value += 1;
            }

            if (tpApps.IsChecked == true) {
                tasks.Add(Task.Run(() => Tools.ThirdPartyUpdater()));
                resultsProgress.Report("\nThird Party Apps Updating");
                ProgressBar2.Value += 1;
            }

            if (bloatkiller.IsChecked == true) {
                Tool tool = resources.bloatkiller;

                // Add to download List
                report.DownloadPercentagesList.Add(tool);

                // create progress reporter to update specific value
                IProgress<int> progressVal = new Progress<int>(value => {
                    // Update our tool refrence
                    tool.PercentageComplete = value;

                    // send updated report model to event
                    progress.Report(report);
                });

                tasks.Add(Downloader.StartDownload(
                    tool,
                    progressVal,
                    progressBarProgress,
                    resultsProgress,
                    true, false, true
                ));
            }

            // Run misc. installers
            foreach(Installers.installer installer in Installers.misc_installers)
                if(installer.box.IsChecked == true)
                    tasks.Add(Task.Run(() => Installers.RunInstaller(
                        installer.path,
                        resultsProgress
                    )));
        }

        private void AddProgressTask(Tool tool) {
            // Check if we are downloading / installing / running our tasks
            bool downloadBool = (bool)download.IsChecked;
            bool installBool = (bool)install.IsChecked;
            bool runBool = (bool)run.IsChecked;

            // Add to download List
            report.DownloadPercentagesList.Add(tool);

            // create progress reporter to update specific value
            IProgress<int> progressVal = new Progress<int>(value => {
                // Update our tool refrence
                tool.PercentageComplete = value;

                // send updated report model to event
                progress.Report(report);
            });

            // add task with reporters
            tasks.Add(Downloader.StartDownload(
                tool,
                progressVal,
                progressBarProgress,
                resultsProgress,
                downloadBool, installBool, runBool
            ));
        }

        /// <summary>
        /// UI Functionality
        /// </summary>

        // make top bar able to drag window
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        // close window
        private void ExitBtn_Click(object sender, EventArgs e) {
            Close();
        }

        // minimize window
        private void Minimize_Click(object sender, EventArgs e) {
            WindowState = WindowState.Minimized;
        }

        // copy asset info to clipboard
        private void CopyBtn_Click(object sender, EventArgs e) {
            try {
                System.Windows.Clipboard.SetText(ComputerInfo.asset);
            } catch {
                ProgressText.Text = "Stop Pressing the Copy Button!";
            }
        }

        // export asset info to {current directory}\asset.txt
        private void ExportBtn_Click(object sender, EventArgs e) {
            try {
                File.WriteAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "asset.txt"),
                    ComputerInfo.asset
                );
                ProgressText.Text = $"Created {Directory.GetCurrentDirectory()}\\asset.txt";
            } catch {
                ProgressText.Text = $"Failed to create {Directory.GetCurrentDirectory()}\\asset.txt";
            }
        }

        // if checked enable child checkboxes else disable children
        private void CBSwitch_Click(object sender, RoutedEventArgs e) {
            if (cbSwitch.IsChecked == false) {
                adw.IsEnabled = false;
                adw.IsChecked = false;
                adw.Opacity = .1;

                mb.IsEnabled = false;
                mb.IsChecked = false;
                mb.Opacity = .1;

                gu.IsEnabled = false;
                gu.IsChecked = false;
                gu.Opacity = .1;
            }
            else if (cbSwitch.IsChecked == true) {
                adw.IsEnabled = true;
                adw.Opacity = 1;

                mb.IsEnabled = true;
                mb.Opacity = 1;

                gu.IsEnabled = true;
                gu.Opacity = 1;
            }
        }

        private static Boolean firstLaunch = true;
        private static Boolean percyWrenchVisible = false;
        private void TogglePercy(object sender, RoutedEventArgs e) {
            if(firstLaunch) {
                firstLaunch = false;
                return;
            }

            if (percyWrenchVisible) {
                PercyWrench.Opacity = 0;
                PercyClipboard.Opacity = 0.5;
                percyWrenchVisible = false;
            } else {
                PercyClipboard.Opacity = 0;
                PercyWrench.Opacity = 0.5;
                percyWrenchVisible = true;
            }
        }

        /// <summary>
        /// Main Window Animation
        /// </summary>

        // on tick specified in "OnLoad" func
        private void timer_Tick(object sender, EventArgs e) {

        }

        // on event call update top progress bar
        private void ReportProgress(object sender, ProgressReportModel e) {
            if (ProgressBar1.Value < e.TotalPercentageCompleted()) {
                ProgressBar1.Value = e.TotalPercentageCompleted();
            }
            ProgressText.Text = $"{e.DownloadCompleted}/{e.DownloadCount} items - Download {e.TotalPercentageCompleted()}% complete";
        }
    }    
}