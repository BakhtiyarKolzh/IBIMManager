using System.Diagnostics;
using System.Management;


namespace Authorization.Services
{
    internal sealed class HardDriveInfo
    {
        internal static string GetMainHardSerialNumber()
        {
            string result = null;
            ManagementClass driveClass = new ManagementClass("Win32_DiskDrive");
            foreach (ManagementObject drive in driveClass.GetInstances())
            {
                if (drive != null)
                {
                    foreach (PropertyData property in drive.Properties)
                    {
                        if (property.Name == "serialNumber")
                        {
                            Debug.Print($"Property: {property.Name}, Value: {property.Value}");
                            result = property.Value.ToString();
                            result.Normalize();
                            break;
                        }
                    }
                    break;
                }
            }
            return result;
        }
    }

}
