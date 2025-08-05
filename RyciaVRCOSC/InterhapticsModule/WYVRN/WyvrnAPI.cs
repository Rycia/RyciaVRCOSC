#region Using Imports
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
#endregion

namespace RyciaVRCOSC.InterhapticsModule.Haptics.WYVRN

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

        public uint SupportedDevice;
        public uint Category;
    }

    public static class WyvrnAPI
    {
        static bool _sIsChromaticAvailable = false;

        private delegate int PluginCoreInitSDKDelegate(ref APPINFOTYPE appInfo);
        private delegate int PluginCoreSetEventNameDelegate(nint name);
        private delegate int PluginCoreUnInitDelegate();

        private static PluginCoreInitSDKDelegate? _pluginCoreInitSDK;
        private static PluginCoreSetEventNameDelegate? _pluginCoreSetEventName;
        private static PluginCoreUnInitDelegate? _pluginCoreUnInit;

        private static nint _libraryHandle = nint.Zero;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetProcAddress(nint hModule, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(nint hModule);

        public static int WyvrnWrapperInitSDK()
        {
            APPINFOTYPE appInfo = new APPINFOTYPE
            {
                Title = "InterhapticsOSC",
                Description = "Connects VRChat OSC parameters to Razer Sensa/Hypersense devices via WYVRN",
                Author_Name = "Rycia",
                Author_Contact = "https://github.com/Rycia/RyciaVRCOSC",
                Category = 1,
                SupportedDevice = 63
            };

            return CoreInitSDK(ref appInfo);
        }

        static WyvrnAPI()
        {
            _sIsChromaticAvailable = false;

            try
            {
                string dllPath = Environment.Is64BitProcess
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Razer Chroma SDK", "bin", "RzChromatic64.dll")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Razer Chroma SDK", "bin", "RzChromatic.dll");

                if (!File.Exists(dllPath))
                {
                    RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke($"[Wyvrn] DLL not found at: {dllPath}");
                    return;
                }

                _libraryHandle = LoadLibrary(dllPath);
                if (_libraryHandle == nint.Zero)
                {
                    RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke($"[InWyvrn] Failed to load DLL at: {dllPath}");
                    return;
                }

                nint initPtr = GetProcAddress(_libraryHandle, "PluginCoreInitSDK");
                nint setEventPtr = GetProcAddress(_libraryHandle, "PluginCoreSetEventName");
                nint uninitPtr = GetProcAddress(_libraryHandle, "PluginCoreUnInit");

                if (initPtr == nint.Zero || setEventPtr == nint.Zero || uninitPtr == nint.Zero)
                {
                    RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke("[Wyvrn] Failed to locate one or more required DLL methods.");
                    return;
                }

                _pluginCoreInitSDK = Marshal.GetDelegateForFunctionPointer<PluginCoreInitSDKDelegate>(initPtr);
                _pluginCoreSetEventName = Marshal.GetDelegateForFunctionPointer<PluginCoreSetEventNameDelegate>(setEventPtr);
                _pluginCoreUnInit = Marshal.GetDelegateForFunctionPointer<PluginCoreUnInitDelegate>(uninitPtr);

                _sIsChromaticAvailable = true;
            }
            catch (Exception ex)
            {
                RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke($"[Wyvrn] Exception during DLL loading: {ex}");
            }
        }

        private static bool IsProductionVersionAvailable(string fileName)
        {
            try
            {
                FileInfo fi = new FileInfo(fileName);
                if (!fi.Exists)
                {
                    return false;
                }

                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(fileName);
                string fileVersion = versionInfo.FileVersion;
                RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke($"ChromaSDK Version={fileVersion} File={fileName}");

                string[] parts = fileVersion.Split('.');
                if (parts.Length < 4 ||
                    !int.TryParse(parts[0], out int major) ||
                    !int.TryParse(parts[1], out int minor) ||
                    !int.TryParse(parts[2], out int build) ||
                    !int.TryParse(parts[3], out int revision))
                {
                    return false;
                }

                return major > 2 ||
                       major == 2 && minor > 0 ||
                       major == 2 && minor == 0 && build > 2 ||
                       major == 2 && minor == 0 && build == 2 && revision >= 0;
            }
            catch (Exception ex)
            {
                string errorMessage = $"[Rycia.Interhaptics.WYVRN] [ERROR] The ChromaSDK is not available! Exception={ex}";
                Console.WriteLine(errorMessage);
                RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke(errorMessage);
                return false;
            }
        }

        public static bool IsWyvrnSDKAvailable()
        {
            return _sIsChromaticAvailable;
        }

        private static nint GetUnicodeIntPtr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return nint.Zero;
            }
            byte[] array = Encoding.Unicode.GetBytes(str + "\0");
            nint lpData = Marshal.AllocHGlobal(array.Length);
            Marshal.Copy(array, 0, lpData, array.Length);
            return lpData;
        }

        private static void FreeIntPtr(nint lpData)
        {
            if (lpData != nint.Zero)
            {
                Marshal.FreeHGlobal(lpData);
            }
        }

        private static bool _sInitialized = false;

        public static int CoreInitSDK(ref APPINFOTYPE appInfo)
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
            int result = _pluginCoreInitSDK != null ? _pluginCoreInitSDK(ref appInfo) : -1;
            if (result == RazerErrors.RZRESULT_SUCCESS)
            {
                _sInitialized = true;
            }
            return result;
        }

        public static int CoreSetEventName(string name)
        {
            if (!_sIsChromaticAvailable || !_sInitialized || _pluginCoreSetEventName == null)
            {
                return -1;
            }
            nint lp_Name = GetUnicodeIntPtr(name);
            int result = _pluginCoreSetEventName(lp_Name);
            FreeIntPtr(lp_Name);
            return result;
        }

        public static int CoreUnInit()
        {
            if (!_sIsChromaticAvailable || !_sInitialized || _pluginCoreUnInit == null)
            {
                return RazerErrors.RZRESULT_SUCCESS;
            }
            int result = _pluginCoreUnInit();
            if (result == RazerErrors.RZRESULT_SUCCESS)
            {
                _sInitialized = false;
            }
            return result;
        }
    }
}