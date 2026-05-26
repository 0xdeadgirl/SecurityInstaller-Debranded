using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Handlers;
using System.Net.Http;
using System.Threading.Tasks;

public static class Downloader {
    private static readonly HttpClientHandler handler = new HttpClientHandler() { AllowAutoRedirect = true };
    private static readonly ProgressMessageHandler ph = new ProgressMessageHandler(handler);
    private static readonly HttpClient client = new HttpClient(ph);

    public static async Task<bool> StartDownload(
        Tool tool,
        IProgress<int> progressVal,
        IProgress<int> progressBar2,
        IProgress<string> results,
        bool download, bool install, bool run
    ) {
        Stopwatch watch = Stopwatch.StartNew();

        ph.HttpReceiveProgress += (_, args) => {
            progressVal.Report(args.ProgressPercentage);
        };

        string filePath = Path.Combine(Directory.GetCurrentDirectory(), tool.ToolName);

        if (download == true) {
            string rez = await Download(tool.ToolUrl, filePath);

            results.Report($"\n{tool.ToolName} download {rez}\nElapsed Time: {watch.ElapsedMilliseconds}ms");
        }

        if(install == true) {
            if (tool.ToolName != "ADWCleaner.exe" && tool.ToolName != "Remote.msi") {
                string rez = await Task.Run(() => Install(tool.ToolName, tool.ToolCliSwitch, results));

                results.Report($"{tool.ToolName} install {rez}\nElapsed Time: {watch.ElapsedMilliseconds}ms");
            } else if (tool.ToolName == "ADWCleaner.exe") {
                Directory.CreateDirectory(@"C:\AdwCleaner");
                File.Copy(
                    Path.Combine(Directory.GetCurrentDirectory(), "ADWCleaner.exe"),
                    @"C:\AdwCleaner\ADWCleaner.exe",
                    true
                );
            }
        }

        if (run == true)
        {
            try
            {
                if (tool.ToolName == "ADWCleaner.exe" || tool.ToolName == "bloatkiller.bat") {
                    await Task.Run(() => Process.Start(tool.ToolLocation, tool.ToolCliSwitch));

                    results.Report($"\n{tool.ToolName} opened in: {watch.ElapsedMilliseconds}ms");
                } else {
                    await Task.Run(() => Process.Start(tool.ToolLocation));

                    results.Report($"\n{tool.ToolName} opened in: {watch.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex) {
                results.Report($"\nError Opening:\n{ex.Message}");
            }
        }

        watch.Reset();
        progressBar2.Report(1);
        return true;
    }

    private static async Task<string> Download(Uri url, string filePath) {
        try {
            using (Stream rez = await client.GetStreamAsync(url)) {
                using (FileStream fs = new FileStream(filePath, FileMode.Create)) {
                    await rez.CopyToAsync(fs);
                }
            }
            return "complete.";
        } catch (Exception ex) {
            return $"not complete.\nReason: {ex.Message}";
        }
    }

    private static string Install(string fileName, string installSwitch, IProgress<string> results) {
        try {
            string output = string.Empty;
            using (Process process = new Process()) {
                // Install Silently
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = installSwitch;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                StreamReader reader = process.StandardOutput;
                output = reader.ReadToEnd();

                results.Report(output);

                process.WaitForExit();
            }

            return $"complete.\nResults:{output}";
        } catch (Exception ex) {
            return $"not complete.\nReason: {ex.Message}";
        }
    }
}