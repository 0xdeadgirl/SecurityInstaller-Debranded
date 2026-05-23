using System;
using System.IO;
using System.Management;
using Microsoft.Win32;

public static class ComputerInfo {
    // Drive Partitions
    private static string GetDriveInfo() {
        string dInfo = string.Empty;

        try {
            DriveInfo[] allDrives = System.IO.DriveInfo.GetDrives();

            foreach (DriveInfo drive in allDrives) {
                if(drive.DriveType != DriveType.Fixed)
                    continue;

                string freeSpace = Bytes.Format(drive.TotalFreeSpace);
                string totalSpace = Bytes.Format(drive.TotalSize);
                string format = drive.DriveFormat;
                string name = drive.Name;

                dInfo += ($"{name} {format} - {freeSpace} free of: {totalSpace}\n");
            }
        } catch (Exception ex) { return ex.Message; }

        return dInfo;
    }

    // ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive")
    private static string GetDriveInfo2() {
        string dInfo = string.Empty;

        try {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive")) {
                foreach (ManagementObject wmi in searcher.Get()) {
                    string size = Bytes.Format(Convert.ToInt64(wmi["Size"]));

                    dInfo += $"{(string)wmi["Model"]} {size}\n";
                    wmi.Dispose();
                }
            }
        } catch (Exception ex) { return ex.Message; }


        return dInfo;
    }

    // ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController")
    private static string GetGPUInfo() {
        string gpuInfo = string.Empty;

        try {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController")) {
                foreach (ManagementObject wmi in searcher.Get()) {
                    gpuInfo += $"{(string)wmi["Name"]}\n";

                    wmi.Dispose();
                }
            }
        } catch (Exception ex) { return ex.Message; }

        return gpuInfo;
    }

    // ManagementClass("Win32_NetworkAdapterConfiguration")
    private static string GetMacAddress() {
        string MACAddress = string.Empty;

        try {
            using (ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration")) {
                using (ManagementObjectCollection moc = mc.GetInstances()) {
                    foreach (ManagementObject mo in moc) {
                        if ((bool)mo["IPEnabled"] == true) {
                            object desc = mo["Description"];
                            object mac = mo["MACAddress"];

                            MACAddress += $"Name: {desc}\nMac: {mac}\n";
                        }

                        mo.Dispose();
                    }
                }
            }
        }
        catch (Exception ex) { return ex.Message; }

        return MACAddress;
    }

    // ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BaseBoard")
    private static string[] GetBoardInfo() {
        string boardMaker = string.Empty;
        string boardInfo = string.Empty;

        try {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BaseBoard")) {
                foreach (ManagementObject wmi in searcher.Get()) {
                    boardMaker = (string)wmi.GetPropertyValue("Manufacturer");
                    boardInfo = (string)wmi.GetPropertyValue("Product");

                    wmi.Dispose();
                }
            }
        } catch (Exception ex) { return new String[] { ex.Message, ex.Message }; }

        return new string[] { boardMaker, boardInfo };
    }

    // ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS")
    private static string[] GetBiosInfo() {
        string biosMaker = string.Empty;
        string biosSerial = string.Empty;
        string biosCaption = string.Empty;

        try {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS")) {
                foreach (ManagementObject wmi in searcher.Get()) {
                    biosMaker = (string)wmi.GetPropertyValue("Manufacturer");
                    biosSerial = (string)wmi.GetPropertyValue("SerialNumber");
                    biosCaption = (string)wmi.GetPropertyValue("Caption");

                    wmi.Dispose();
                }
            }
        }
        catch (Exception ex) { return new string[] { ex.Message, ex.Message, ex.Message }; }

        return new string[] { biosMaker, biosSerial, biosCaption };
    }

    // ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_ComputerSystemProduct")
    private static string GetProductName() {
        string productName = string.Empty;

        try {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_ComputerSystemProduct")) {
                foreach (ManagementObject wmi in searcher.Get()) {
                    productName = (string)wmi.GetPropertyValue("Name");

                    wmi.Dispose();
                }
            }
        } catch (Exception ex) { return ex.Message; }

        return productName;
    }

    // ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory")
    private static string GetMemory() {
        string memSizestring = string.Empty;
        ManagementScope oMs = new ManagementScope();
        ObjectQuery oQuery = new ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory");

        try {
            using (ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery)) {
                using (ManagementObjectCollection oCollection = oSearcher.Get()) {
                    long memorySize = 0;
                    long mCap = 0;

                    // In case more than one Memory sticks are installed
                    foreach (ManagementObject obj in oCollection) {
                        mCap = Convert.ToInt64(obj["Capacity"]);
                        memorySize += mCap;

                        obj.Dispose();
                    }

                    memSizestring = Bytes.Format(memorySize);
                }
            }
        } catch (Exception ex) { return ex.Message; }

        return memSizestring;
    }

    // ObjectQuery oQuery2 = new ObjectQuery("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray")
    private static string GetRamSlots() {
        string memSlotsString = string.Empty;
        ManagementScope oMs = new ManagementScope();
        ObjectQuery oQuery2 = new ObjectQuery("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray");

        try {
            using (ManagementObjectSearcher oSearcher2 = new ManagementObjectSearcher(oMs, oQuery2)) {
                using (ManagementObjectCollection oCollection2 = oSearcher2.Get()) {
                    int memSlots = 0;
                    foreach (ManagementObject obj in oCollection2) {
                        memSlots = Convert.ToInt32(obj["MemoryDevices"]);

                        obj.Dispose();
                    }
                    memSlotsString = memSlots.ToString();
                }
            }
        } catch (Exception ex) { return ex.Message; }

        return memSlotsString;
    }

    private static string GetRamSpeed() {
        int minSpeed = -1;
        ManagementScope oMs = new ManagementScope();
        ObjectQuery oQuery2 = new ObjectQuery("SELECT Speed FROM Win32_PhysicalMemory");

        try {
            using (ManagementObjectSearcher oSearcher2 = new ManagementObjectSearcher(oMs, oQuery2)) {
                using (ManagementObjectCollection oCollection2 = oSearcher2.Get()) {
                    foreach (ManagementObject obj in oCollection2) {
                        int speed = Convert.ToInt32(obj["Speed"]);
                        if (minSpeed == -1)
                            minSpeed = speed;
                        else if (speed < minSpeed)
                            minSpeed = speed;

                            obj.Dispose();
                    }
                }
            }
        } catch (Exception ex) { return ex.Message; }

        return minSpeed.ToString() + "MHz";
    }

    // ManagementClass mc = new ManagementClass("Win32_Processor")
    private static string GetCpuInfo() {
        string cpuMan = string.Empty;
        string info = string.Empty;
        double? GHz = 0;

        try {
            using (ManagementClass mc = new ManagementClass("Win32_Processor")) {
                foreach (ManagementObject mo in mc.GetInstances()) {
                    GHz = 0.001 * (UInt32)mo.Properties["MaxClockSpeed"].Value;

                    string name = (string)mo["Name"];
                    name = name.Replace("(TM)", "™")
                        .Replace("(tm)", "™")
                        .Replace("(R)", "®")
                        .Replace("(r)", "®")
                        .Replace("(C)", "©")
                        .Replace("(c)", "©")
                        .Replace("    ", " ")
                        .Replace("  ", " ");

                    info = name;

                    mo.Dispose();
                    break;
                }

                using (ManagementObjectCollection objCol = mc.GetInstances()) {
                    foreach (ManagementObject obj in objCol) {
                        if (cpuMan == String.Empty) {
                            cpuMan = (string)obj.Properties["Manufacturer"].Value;
                        }

                        obj.Dispose();
                    }
                }
            }

        }
        catch (Exception ex) { return ex.Message; }

        return $"{cpuMan}\n{info}\n{GHz}";
    }

    private static string GetWinDisplayVersion() {
        using(RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")) {
            try {
                if(key != null)
                    return key.GetValue("DisplayVersion").ToString() ?? "N/A";
            } catch(Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        return "N/A";
    }

    // ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem")
    private static string GetOSInformation() {
        string osInfo = string.Empty;

        try {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem")) {
                foreach (ManagementObject wmi in searcher.Get()) {
                    osInfo = $"{((string)wmi["Caption"]).Trim()}\nVersion: {(string)GetWinDisplayVersion()}\nBuild: {(string)wmi["Version"]}\nArchitecture: {(string)wmi["OSArchitecture"]}";

                    wmi.Dispose();
                }
            }
        } catch (Exception ex) { return ex.Message; }

        return osInfo;
    }

    // ManagementClass("Win32_ComputerSystem")
    private static string GetSystemSKU() {
        string info = string.Empty;

        try {
            using (ManagementClass mc = new ManagementClass("Win32_ComputerSystem")) {
                using (ManagementObjectCollection moc = mc.GetInstances()) {
                    foreach (ManagementObject mo in moc) {
                        info = (string)mo["SystemSKUNumber"];
                    }
                }
            }
        } catch (Exception ex) { return ex.Message; }

        return info;
    }

    ///
    /// Variables
    ///

    public static readonly string OsInfo = GetOSInformation();
    public static readonly string[] BoardInfo = GetBoardInfo(); // { boardMaker, boardInfo }
    public static readonly string[] BiosInfo = GetBiosInfo(); // { biosMaker, biosSerial, biosCaption }
    public static readonly string ProductName = GetProductName();
    public static readonly string SystemSku = GetSystemSKU();
    public static readonly string RamSlots = GetRamSlots();
    public static readonly string MaxMemory = GetMemory();
    public static readonly string RamSpeed = GetRamSpeed();
    public static readonly string CPU = GetCpuInfo();
    public static readonly string GPU = GetGPUInfo();
    public static readonly string Partitions = GetDriveInfo();
    public static readonly string Drives = GetDriveInfo2();
    public static readonly string Mac = GetMacAddress();

    ///
    /// Tidy up
    ///

    public static string asset = $@"
--------------------------------------------------------
        Host Name
--------------------------------------------------------
    
{"\tDevice:\t" + Environment.MachineName}
{"\tUser:\t" + Environment.UserName}

            
--------------------------------------------------------
        Operating System
--------------------------------------------------------
    
{"\t" + OsInfo}


--------------------------------------------------------
        Device Brand & Model
--------------------------------------------------------
    
{"\t" + BoardInfo[0]}
{"\t" + ProductName}


--------------------------------------------------------
        Bios Make & Model
--------------------------------------------------------
    
{"\t" + BiosInfo[0]}
{"\t" + BoardInfo[1]}
{"\tVer: " + BiosInfo[2]}


--------------------------------------------------------
        Serial Number
--------------------------------------------------------
    
{"\tSerial:\t" + BiosInfo[1]}
{"\tSKU:\t" + SystemSku}


--------------------------------------------------------
        Memory
--------------------------------------------------------
    
{"\tSticks: " + RamSlots}
{"\tTotal: " + MaxMemory}
{"\tSpeed: " + RamSpeed}

            
--------------------------------------------------------
        CPU
--------------------------------------------------------
    
{"\t" + CPU}


--------------------------------------------------------
        GPU
--------------------------------------------------------
    
{"\t" + GPU}

--------------------------------------------------------
        DiskInfo
--------------------------------------------------------
    
    Partitions:
{"\t" + Partitions}
    Local / Physical Drives:
{"\t" + Drives}

--------------------------------------------------------
        Mac Address
--------------------------------------------------------

{Mac}

";
}