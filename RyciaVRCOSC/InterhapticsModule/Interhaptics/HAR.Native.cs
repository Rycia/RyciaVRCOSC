/* ​
* Copyright (c) 2024 Go Touch VR SAS. All rights reserved. ​
* ​https://doc.wyvrn.com/docs/interhaptics-sdk/haptics-sdk-for-game-engines/overview/
*/

using System;
using System.IO;
using System.Runtime.InteropServices;
//using Interhaptics.HapticBodyMapping; // Not needed for VROSC

namespace RyciaVRCOSC.InterhapticsModule.Interhaptics
{

	public static partial class HAR
	{
        #region DLL Import
        private static readonly IntPtr _libraryHandle = IntPtr.Zero;

		private static readonly string RemoteDllPath = Path.Combine( // Use github repo path before the local build path
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			@"VRCOSC\packages\remote\rycia.vrcosc.modules\HAR.dll"
		);

		private static readonly string LocalDllPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			@"VRCOSC\packages\local\HAR.dll"
		);
        const string DLL_NAME = "HAR";

		static HAR()
		{
			if (!Environment.Is64BitProcess) //Only support 64 bit
            {
				Log("[Rycia.Interhaptics.HAR] [ERROR] Attempted to load HAR.dll in 32-bit when 64-bit is expected."); 
				return;
			}

			try
			{
				string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string remotePath = Path.Combine(basePath, RemoteDllPath);
				string localPath = Path.Combine(basePath, LocalDllPath);

				string dllPath = File.Exists(remotePath) ? remotePath : localPath;

				if (!File.Exists(dllPath))
				{
					Log($"[Rycia.Interhaptics.HAR] [ERROR] HAR.dll not found at: {dllPath}");
					return;
				}

				_libraryHandle = LoadLibrary(dllPath);
				if (_libraryHandle == IntPtr.Zero)
				{
					Log($"[Rycia.Interhaptics.HAR] [ERROR] Failed to load HAR.dll at: {dllPath}");
					return;
				}

				
				IntPtr initPtr = GetProcAddress(_libraryHandle, "Init"); // Optional: resolve critical function to ensure load succeeded properly
                if (initPtr == IntPtr.Zero)
				{
					Log("[Rycia.Interhaptics.HAR] [ERROR] Failed to locate Init() in HAR.dll.");
					return;
				}
				
                LogDebug("[Rycia.Interhaptics.HAR] [INFO] HAR.dll loaded sucessfully and is ready to use."); // Ready to use
            }
			catch (Exception ex)
			{
				Log($"[Rycia.Interhaptics.HAR] [ERROR] Exception during HAR.dll loading: {ex}");
			}
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32.dll", SetLastError = true)]

		#endregion
		private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport(DLL_NAME)] public static extern bool ProviderInit();
        [DllImport(DLL_NAME)] public static extern bool ProviderIsPresent();
        [DllImport(DLL_NAME)] public static extern bool ProviderClean();
        [DllImport(DLL_NAME)] public static extern void ProviderRenderHaptics();

        /// Initializes the different components and modules of the Interhaptics Engine.
        /// - Haptic Material Manager: module in charge of loading and storing haptic effects
        /// - Human Avatar Manager: module in charge of mapping between device, human avatar, and experience
        /// - Haptic Event Manager: module in charge of the control of haptic sources
        /// <returns>Always true even if a module is not properly initialized.</returns>
        [DllImport(DLL_NAME)] public static extern bool Init();
		/// Cleans the different components and modules of the Interhaptics Engine.
		/// To be called before the application is quit.
		[DllImport(DLL_NAME)] public static extern void Quit();
		/// Sets the global rendering intensity factor for the whole engine.
		/// <param name="_intensity">Positive value. 0 is nothing. Base value is 1.</param>
		[DllImport(DLL_NAME)] public static extern void SetGlobalIntensity(double _intensity);
		/// Gets the global rendering intensity factor for the whole engine.
		/// <returns>The global intensity. -1 if mixer is not initialized.</returns>
		[DllImport(DLL_NAME)] public static extern double GetGlobalIntensity();
		/// Creates a parametric effect using amplitude, pitch, and transient parameters.
		/// <param name="_amplitude">Array of amplitude values formatted as Time - Value pairs, with values between 0 and 1.</param>
		/// <param name="_amplitudeSize">Size of the amplitude array.</param>
		/// <param name="_pitch">Array of pitch values formatted as Time - Value pairs, with values between 0 and 1.</param>
		/// <param name="_pitchSize">Size of the pitch array.</param>
		/// <param name="_freqMin">Minimum value for the frequency range.</param>
		/// <param name="_freqMax">Maximum value for the frequency range.</param>
		/// <param name="_transient">Array of transient values formatted as Time - Amplitude - Frequency triples, with values between 0 and 1.</param>
		/// <param name="_transientSize">Size of the transient array.</param>
		/// <param name="_isLooping">Indicates whether the effect should loop.</param>
		/// <returns>ID of the created haptic source, or -1 if creation failed.</returns>
		[DllImport(DLL_NAME)] public static extern int AddParametricEffect([In] double[] _amplitude, int _amplitudeSize, [In] double[] _pitch, int _pitchSize, double _freqMin, double _freqMax, [In] double[] _transient, int _transientSize, bool _isLooping);
		/// Sets the haptic intensity factor for a specific source.
		/// <param name="_hMaterialId">ID of the source. Same as the attached haptic effect.</param>
		/// <param name="_intensity">Intensity factor value. Always clamped above 0.</param>
		[DllImport(DLL_NAME)] public static extern void SetEventIntensity(int _hMaterialId, double _intensity);
		/// Sets the loop flag for a specific source
		/// <param name="_hMaterialID">ID of the source. Same as the attached haptic effect.</param>
		/// <param name="_numberOfLoops">Number of loops for the event. 0 means the source is not looping, 1 or more is the number of loops set</param>
		/// <returns></returns>
		[DllImport(DLL_NAME)] public static extern void SetEventLoop(int _hMaterialID, int _numberOfLoops);
		/// Updates the haptic intensity for a specific target of a source.
		/// <param name="_hMaterialId">ID of the source. Same as the attached haptic effect.</param>
		/// <param name="_target">Array of CommandData representing the target. A target contains a group of body parts, lateral flags, and exclusion flags.</param>
		/// <param name="_size">Size of the _target array.</param>
		/// <param name="_intensity">New intensity factor value. Always clamped above 0.</param>
		//[DllImport(DLL_NAME)] public static extern void SetTargetIntensityMarshal(int _hMaterialId, Interhaptics.HapticBodyMapping.CommandData[] _target, int _size, double _intensity);
		/// Adds the content of an .haps file to the Interhaptics Engine for future use.
		/// <param name="_content">JSON content of the .haps file to be loaded. Needs to follow Interhaptics .haps format.</param>
		/// <returns>ID of the haptic effect to be used in other engine calls. -1 if loading failed.</returns>
		[DllImport(DLL_NAME)] private static extern int AddHM(string _content);
		/// Replaces the content of an already loaded haptic effect.
		/// Useful when the ID of the haptic effect needs to be persistent.
		/// <param name="_hMaterialID">ID of the haptic effect to be replaced.</param>
		/// <param name="_content">JSON content of the .haps file to be loaded. Needs to follow Interhaptics .haps format.</param>
		/// <returns>true if the effect was properly updated, false otherwise.</returns>
		[DllImport(DLL_NAME)] private static extern bool UpdateHM(int _id, string _content);

		[DllImport(DLL_NAME)] public static extern double GetVibrationAmp(int _id, double _step, int _mode = 0);

		[DllImport(DLL_NAME)] public static extern double GetVibrationFreq(int _id, double _step);
		/// Retrieves the length of the vibration for a given haptic effect.
		/// <param name="_id">The identifier for the haptic effect.</param>
		/// <returns>The length of the vibration.</returns>
		[DllImport(DLL_NAME)] public static extern double GetVibrationLength(int _id);
		[DllImport(DLL_NAME)] public static extern double GetTextureAmp(int _id, double _step, int _mode = 0);
		[DllImport(DLL_NAME)] public static extern double GetTextureFreq(int _id, double _step);
		[DllImport(DLL_NAME)] public static extern double GetTextureLength(int _id);
		[DllImport(DLL_NAME)] public static extern double GetStiffnessAmp(int _id, double _step);
		[DllImport(DLL_NAME)] public static extern double GetStiffnessFreq(int _id, double _step);
		/// ClearOutputBuffers
		/// <param name="_resetClips">Set to true to also clear the clips</param>
		[DllImport(DLL_NAME)] public static extern void ClearOutputBuffers(bool _resetClips = false);
		/// Starts the rendering playback of a haptic source. Sets the starting time to 0 + different offsets.
		/// If the event is already playing, it restarts with the new offsets. If the source does not already exist, it will be created.
		/// <param name="_hMaterialId">ID of the source to play. Same as the attached haptic effect.</param>
		/// <param name="_vibrationOffset">Vibration offset.</param>
		/// <param name="_textureOffset">Texture offset.</param>
		/// <param name="_stiffnessOffset">Stiffness offset.</param>
		[DllImport(DLL_NAME)] public static extern void PlayEvent(int _hMaterialId, double _vibrationOffset, double _textureOffset, double _stiffnessOffset);
		/// Stops the rendering playback of a haptic source.
		/// <param name="_hMaterialId">ID of the source to stop. Same as the attached haptic effect.</param>
		[DllImport(DLL_NAME)] public static extern void StopEvent(int _hMaterialId);
		/// Stops the rendering playback of all haptic sources. Active Events become inactive. Inactive events are cleared from memory.
		[DllImport(DLL_NAME)] public static extern void StopAllEvents();
		[DllImport(DLL_NAME)] public static extern void ClearActiveEvents();
        /// Removes all active sources from the memory. Deleted sources can be recreated by calling the PlayEvent function.

        [DllImport(DLL_NAME)] public static extern void ClearInactiveEvents();
        /// Removes all inactive sources from the memory. Inactive sources are kept in memory to avoid deletion
        /// and creation when playing and stopping a source.

        /// Clears a specific haptic source whether it is active or not.
        /// <param name="_hMaterialId">ID of the source. Same as the attached haptic effect.</param>
        [DllImport(DLL_NAME)] public static extern void ClearEvent(int _hMaterialId);

        /// Sets the offsets for a specific haptic source.
        /// <param name="_hMaterialId">ID of the source. Same as the attached haptic effect.</param>
        /// <param name="_vibrationOffset">Vibration offset.</param>
        /// <param name="_textureOffset">Texture offset.</param>
        /// <param name="_stiffnessOffset">Stiffness offset.</param>
        [DllImport(DLL_NAME)] public static extern void SetEventOffsets(int _hMaterialId, double _vibrationOffset, double _textureOffset, double _stiffnessOffset);

        /// Updates the spatial positions for a specific source target.
        /// <param name="_hMaterialId">ID of the source. Same as the attached haptic effect.</param>
        /// <param name="_target">Array of CommandData to build a target. A target contains a group of body parts, lateral flags, and exclusion flags.</param>
        /// <param name="_size">Size of the _target array.</param>
        /// <param name="_texturePosition">New texture position.</param>
        /// <param name="_stiffnessPosition">New stiffness position.</param>
		/// To be called in the application main loop to trigger the rendering of all haptic buffers at a specific time.
		/// The Interhaptics Engine will compare the current time with the last known value to build a buffer large enough to cover frame drops.
		/// Can be called from the main thread or in a parallel loop. Must be called at least once before triggering the device update event.
		/// <param name="_curTime">Current time in seconds.</param>
		[DllImport(DLL_NAME)] public static extern void ComputeAllEvents(double _curTime);
		/// Adds a target in range of the source.
		/// The Interhaptics Engine will remap device endpoints and in-range target to the device management layer for haptic playback.
		/// <param name="_hMaterialId">ID of the source to add a target to. Same as the attached haptic effect.</param>
		/// <param name="_target">Pointer of CommandData to build a target. A target contains a group of bodyparts, lateral flags, and exclusion flags. Memory must be allocated before calling this entrypoint.</param>
		/// <param name="_size">Size of the _target array.</param>
		/// Removes a target from a source range.
		/// The Interhaptics Engine will remap device endpoints and in-range target to the device management layer for haptic playback.
		/// <param name="_hMaterialId">ID of the source to remove a target from. Same as the attached haptic effect.</param>
		/// <param name="_target">Array of CommandData to build a target. A target contains a group of body parts, lateral flags, and exclusion flags.</param>
		/// <param name="_size">Size of the _target array.</param>
		/// Removes all targets from a source range.
		/// <param name="_hMaterialId">ID of the source to remove targets from. Same as the attached haptic effect.</param>
		[DllImport(DLL_NAME)] public static extern void RemoveAllTargetsFromEvent(int _hMaterialId);

        #region VRCOSC Logger
        private static void Log(string message) // Shorten 
        {
            RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLogger?.Invoke(message);
        }
		private static void LogDebug(string message) // Shorten 
        {
            RyciaVRCOSC.InterhapticsModule.InterhapticsModule.ExternalLoggerDebug?.Invoke(message);
        }
        #endregion
    }
}