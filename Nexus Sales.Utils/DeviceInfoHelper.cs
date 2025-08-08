using System.Management;
using System;

namespace NexusSales.Utils
{
    public static class DeviceInfoHelper
    {
        public static string GetLaptopSerialNumber()
        {
            try
            {
                // ManagementObjectSearcher is in System.Management.dll
                // Make sure your project references System.Management (right-click References > Add Reference... > Assemblies > Framework > System.Management)
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (var wmi in searcher.Get())
                    {
                        return wmi["SerialNumber"]?.ToString()?.Trim();
                    }
                }
            }
            catch { }
            return "UNKNOWN";
        }
    }
}