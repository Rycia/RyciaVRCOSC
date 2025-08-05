using RyciaVRCOSC.Wyvern;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WyvrnSDK
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct APPINFOTYPE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string Description;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Author_Name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Author_Contact;

        public UInt32 SupportedDevice;
        public UInt32 Category;
    }

    public static class WyvrnAPI
    {
        const string DLL_NAME = "RzChromatic";
        static bool _sIsChromaticAvailable = false;

        public static int WyvrnWrapperInitSDK() // Can call WyvrnWrapperInitSDK() anywhere now.
        {
            APPINFOTYPE appInfo = new APPINFOTYPE
            {
                Title = "InterhapticsOSC",
                Description = "Connects VRChat OSC parameters to Razer Sensa/Hypersense devices via WYVRN",
                Author_Name = "Rycia",
                Author_Contact = "https://github.com/Rycia/RyciaVRCOSC",
                Category = 1, // Utility
                SupportedDevice = 63 // All available devices (bitmask)
            };

            return CoreInitSDK(ref appInfo);
        }

        static WyvrnAPI()

        {
            _sIsChromaticAvailable = false;

            try
            {
                string[] fileNames;
                // check program files for production version
                // check windows systems folders for production version

                fileNames = new string[]{
          
          //Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Razer Chroma SDK", "bin", "RzChromatic.dll"), // Get 32-bit program files folder
          Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "RzChromatic.dll"), // Get SysWOW64 folder
      };

                foreach (string fileName in fileNames)
                {
                    if (!IsProductionVersionAvailable(fileName))
                    {
                        return;
                    }
                }

                _sIsChromaticAvailable = true;  // production version or better
            }
            catch (Exception ex)
            {
                string errorMessage = $"[Rycia.Interhaptics.WYVRN] [ERROR] The ChromaSDK is not available! Exception={ex}";

                Console.WriteLine(errorMessage);
                RyciaVRCOSC.InterhapticsModule.ExternalLogger?.Invoke(errorMessage);
            }
        }

        private
         static bool IsProductionVersionAvailable(string fileName)
        {
            try
            {
                FileInfo fi = new FileInfo(fileName);
                if (!fi.Exists)
                {
                    return false;
                }

                System.Diagnostics.FileVersionInfo versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(fileName);
                string fileVersion = versionInfo.FileVersion;
                RyciaVRCOSC.InterhapticsModule.ExternalLogger?.Invoke(string.Format("ChromaSDK Version={0} File={1}", fileVersion, fileName));

                String[] versionParts = fileVersion.Split(".".ToCharArray());
                if (versionParts.Length < 4)
                {
                    return false;
                }

                int major;
                if (!int.TryParse(versionParts[0], out major))
                {
                    return false;
                }

                int minor;
                if (!int.TryParse(versionParts[1], out minor))
                {
                    return false;
                }

                int build;
                if (!int.TryParse(versionParts[2], out build))
                {
                    return false;
                }

                int revision;
                if (!int.TryParse(versionParts[3], out revision))
                {
                    return false;
                }

                // Anything less than the min version returns false
                // major, minor, build, revision ref: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assemblyversionattribute.-ctor?source=recommendations&view=net-7.0
                const int minMajor = 2;
                const int minMinor = 0;
                const int minBuild = 2;
                const int minRevision = 0;

                if (major < minMajor)  // Less than minMajor
                {
                    return false;
                }

                if (major == minMajor && minor < minMinor)  // Less than minMinor
                {
                    return false;
                }

                if (major == minMajor && minor == minMinor &&
                    build < minBuild)  // Less than minBuild
                {
                    return false;
                }

                if (major == minMajor && minor == minMinor && build == minBuild &&
                    revision < minRevision)  // Less than minRevision
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = $"[Rycia.Interhaptics.WYVRN] [ERROR] The ChromaSDK is not available! Exception={ex}";
                Console.WriteLine(errorMessage);
                RyciaVRCOSC.InterhapticsModule.ExternalLogger?.Invoke(errorMessage);
                return false;
            }
        }

        /// Check if the RzChromatic DLL exists before calling API Methods
        public
         static bool IsWyvrnSDKAvailable()
        { return _sIsChromaticAvailable; }

        #region Helpers(handle path conversions)

        /// Helper to Unicode path string to IntPtr
        private static IntPtr GetUnicodeIntPtr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return IntPtr.Zero;
            }
            byte[] array = UnicodeEncoding.Unicode.GetBytes(str + "\0");
            IntPtr lpData = Marshal.AllocHGlobal(array.Length);
            Marshal.Copy(array, 0, lpData, array.Length);
            return lpData;
        }

        /// Helper to recycle the IntPtr
        private static void FreeIntPtr(IntPtr lpData)
        {
            if (lpData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(lpData);
            }
        }

        #endregion

        #region Public API Methods
        private
         static bool _sInitialized = false;
        /// Direct access to low level API.
        public
         static int CoreInitSDK(ref WyvrnSDK.APPINFOTYPE appInfo)
        {
            appInfo.SupportedDevice = 63;

            if (!_sIsChromaticAvailable)
            {
                return -1;
            }
            if (_sInitialized)
            {
                return RazerErrors.RZRESULT_SUCCESS;
            }
            int result = PluginCoreInitSDK(ref appInfo);
            if (result == RazerErrors.RZRESULT_SUCCESS)
            {
                _sInitialized = true;
            }
            return result;
        }
        public
         static int CoreSetEventName(string name)
        {
            if (!_sIsChromaticAvailable)
            {
                return -1;
            }
            if (!_sInitialized)
            {
                return -1;
            }
            IntPtr lp_Name = GetUnicodeIntPtr(name);
            int result = PluginCoreSetEventName(lp_Name);
            FreeIntPtr(lp_Name);
            return result;
        }
        public
         static int CoreUnInit()
        {
            if (!_sIsChromaticAvailable)
            {
                return RazerErrors.RZRESULT_SUCCESS;
            }
            if (!_sInitialized)
            {
                return RazerErrors.RZRESULT_SUCCESS;
            }
            int result = PluginCoreUnInit();
            if (result == RazerErrors.RZRESULT_SUCCESS)
            {
                _sInitialized = false;
            }
            return result;
        }
        #endregion

        #region Private DLL Hooks
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern int PluginCoreInitSDK(ref WyvrnSDK.APPINFOTYPE appInfo);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern int PluginCoreSetEventName(IntPtr name);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PluginCoreUnInit();
        #endregion
    }
}