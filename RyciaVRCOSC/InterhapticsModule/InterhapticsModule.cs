#region Using Imports
using System;
using System.Threading.Tasks;
using VRCOSC.App.SDK.Modules;
using VRCOSC.App.SDK.Parameters;
using RyciaVRCOSC.InterhapticsModule.Haptics.WYVRN;
//using RyciaVRCOSC.InterhapticsModule.Interhaptics;
#endregion

#region Notes
//using VRCOSC.App.SDK.VRChat;
//using VRCOSC.App.Utils;

//https://vrcosc.com/docs/v2/sdk
//https://github.com/VolcanicArts/VRCOSC-Modules/blob/main/VRCOSC.Modules/Media/MediaModule.cs as example for VRCOSC SDK formatting
//https://github.com/VolcanicArts/VRCOSC-Modules/blob/main/VRCOSC.Modules/AFKDetection/AFKDetectionModule.cs another example for VRCOSC
//https://doc.wyvrn.com/docs/wyvrn-sdk/unity/
#endregion

namespace RyciaVRCOSC.InterhapticsModule;

#region Module Information
[ModuleTitle("Interhaptics")]
[ModuleDescription("Parameter support for Interhaptics-based devices such as Razer headsets with Razer Sensa/Hypersense haptic feedback.")]
[ModuleType(ModuleType.Generic)]
[ModulePrefab("Prefabs", "https://vrcosc.com/docs/downloads#prefabs")] // No prefab yet, change later. dont use.
#endregion
public class InterhapticsModule : Module
{
    #region Program Variables
    private int _mResult = 0; // This is the result on wether or not if Wyvrn successfully loaded up or not. Needs to be here to be global to this class.
    public static System.Action<string>? ExternalLogger; //Exposes the Log() function to WyvrnAPI.CS, which otherwise can't access it for modified extra logging.
    public static System.Action<string>? ExternalLoggerDebug; //Exposes the Log() function to WyvrnAPI.CS, which otherwise can't access it for modified extra logging.
    #endregion

    #region Module Loading
    protected override void OnPreLoad()
    {
        LogDebug("[Rycia.Interhaptics] [DEBUG] OnPreLoad");
        //protected means only this class and its subclasses can access this method. It's not public, so it can't be called from just anywhere.
        //override means this method replaces a method with the same name from a parent/base class, void means it doesnt return anything.
        //OnPreLoad() happens before the module loads the user's data from disk and begins the loading process to get itself ready to be run.

        //VRCOSC SETTINGS - static, required to be OnPreLoad so that a user's settings can be loaded when the module loads. Configs in VRCX
        LogDebug("[Rycia.Interhaptics] [DEBUG] Registering settings.");
        CreateSlider(InterhapticsVRCOSCSetting.Mode,"Mode","How many motors does your haptic hardware have?\n\n" +"If you use a Razer headset, this should be set to 2 (Stereo).\n" +"Other devices may only have one motor, set those to 1 (Mono).\n" +"Multi-motor support may come later.\n\n" +"Default: 2",2,1,2);
        CreateSlider(InterhapticsVRCOSCSetting.Intensity,"Intensity","How intense do you want your haptic feedback?\n\n" +"This is the intensity in linear correlation to the total feedback your device is set to output.\n" +"Set this if you don't want your touch haptics as intense as your sound haptics, which is important for headsets.\n\n" +"Default: 1.0",1f,0f,1f);

        //VRC AVATAR PARAMETERS
        LogDebug("[Rycia.Interhaptics] [DEBUG] Registering parameters.");
        RegisterParameter<float>(InterhapticsVRCOSCParameter.ContactHitRight, "OSC/Interhaptics/ContactHitRight", ParameterMode.ReadWrite, "Stereo - Left Contact", "0-1 float.\nWhen the left contact is touched, goes from 0 to 1 based on distance.\nControls haptic intensity for this side, before multiplying by intensity to hardware.");
        RegisterParameter<float>(InterhapticsVRCOSCParameter.ContactHitLeft, "OSC/Interhaptics/ContactHitLeft", ParameterMode.ReadWrite, "Stereo - Right Contact", "0-1 float.\nWhen the right contact is touched, goes from 0 to 1 based on distance.\nControls haptic intensity for this side, before multiplying by intensity to hardware.");
        RegisterParameter<float>(InterhapticsVRCOSCParameter.ContactHitCenter, "OSC/Interhaptics/ContactHitCenter", ParameterMode.ReadWrite, "Mono - Center Contact", "0-1 float.\nWhen the center contact is touched, goes from 0 to 1 based on distance.\nControls haptic intensity, before multiplying by intensity to hardware. Only matters for single-motor devices.");
    }

    protected override void OnPostLoad()
    {
        LogDebug("[Rycia.Interhaptics] [DEBUG] OnPostLoad");
        ExternalLogger = Log; //Exposes the Log() and LogDebug() function, which otherwise other scripts can't access it for modified extra logging.
        ExternalLoggerDebug = LogDebug;
    }
    #endregion

    #region Module Logic
    protected override Task<bool> OnModuleStart()
    {

        LogDebug("[Rycia.Interhaptics] [DEBUG] OnModuleStart");
        // Async runs asynchronously, it allows to use await inside the method to do things like wait for like file loading
        // Called whenever a user runs the modules, includes the modules being automatically started, only once on start so is the perfect place to do initial setup.

        // Grab and initialize settings
        GetSettingValue<int>(InterhapticsVRCOSCSetting.Mode);      // Obtain mode as raw int
        GetSettingValue<float>(InterhapticsVRCOSCSetting.Intensity); // Obtain intensity as raw float

        //Grab and initialize parameters
        /*
        #region Initialize Interhaptics SDK
        bool harInit = HAR.Init();
        bool providerInit = HAR.ProviderInit();

        if (!harInit)
        {
            Log("[Rycia.Interhaptics] [ERROR] HAR.Init() failed.");
            return Task.FromResult(false);
        }

        if (!providerInit)
        {
            Log("[Rycia.Interhaptics] [ERROR] ProviderInit() failed.");
            return Task.FromResult(false);
        }

        Log("[Rycia.Interhaptics] [INFO] Interhaptics SDK initialized successfully.");
        #endregion
        */
        #region Initialize WYVRN SDK
        if (!WyvrnAPI.IsWyvrnSDKAvailable())
        {
            _mResult = RazerErrors.RZRESULT_DLL_NOT_FOUND;
            Log("[Rycia.Interhaptics] [ERROR] WYVRN SDK not available. DLL not found! Please install Razer Synapse and reboot.");
            return Task.FromResult(false);
        }

        _mResult = WyvrnAPI.WyvrnWrapperInitSDK();

        if (_mResult == RazerErrors.RZRESULT_SUCCESS)
        {
            Log("[Rycia.Interhaptics] [INFO] WYVRN SDK initialized successfully.");
            Task.Delay(100).Wait(); // Give SDK time to stabilize
        }
        else
        {
            Log($"[Rycia.Interhaptics] [ERROR] WYVRN SDK failed to initialize. Error Code: {_mResult}");
            return Task.FromResult(false);
        }
        #endregion

        return Task.FromResult(true);
    }

    /*
    protected override Task OnModuleStop()
    {
        LogDebug("[Rycia.Interhaptics] [DEBUG] OnModuleStop");

        // Deinitialize Interhaptics
        try
        {
            bool providerCleanSuccess = HAR.ProviderClean();
            HAR.Quit();

            if (providerCleanSuccess)
                Log("[Rycia.Interhaptics] [INFO] Interhaptics SDK shut down successfully.");
            else
                Log("[Rycia.Interhaptics] [WARN] ProviderClean() failed during Interhaptics shutdown.");
        }
        catch (Exception ex)
        {
            Log($"[Rycia.Interhaptics] [ERROR] Exception during Interhaptics shutdown: {ex}");
        }

        // Deinitialize WYVRN
        try
        {
            if (_mResult == RazerErrors.RZRESULT_SUCCESS)
            {
                int result = WyvrnAPI.CoreUnInit();
                if (result == RazerErrors.RZRESULT_SUCCESS)
                {
                    Log("[Rycia.Interhaptics] [INFO] WYVRN SDK stopped successfully.");
                }
                else
                {
                    Log($"[Rycia.Interhaptics] [WARN] WYVRN SDK failed to stop. Error Code: {result}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Rycia.Interhaptics] [ERROR] Exception during WYVRN shutdown: {ex}");
        }

        return Task.CompletedTask;
    }
    */
    protected override void OnRegisteredParameterReceived(RegisteredParameter parameter)
    {
        switch (parameter.Lookup)
        {
            case InterhapticsVRCOSCParameter.ContactHitRight:
                {
                    float intensity = GetSettingValue<float>(InterhapticsVRCOSCSetting.Intensity);
                    float scaled = parameter.GetValue<float>() * intensity;
                    LogDebug($"[HIT RIGHT] OSC={parameter.GetValue<float>()} → INTENSITY={scaled}");
                    //WyvrnAPI.CoreSetEventName("Haptic_Right");
                    //WyvrnAPI.PlayConstant(scaled, 0.2, scaled, 1, LateralFlag.Right);
                    WyvrnAPI.CoreSetEventName("RazerKrakenV4Pro_Steady");
                    break;
                }
            case InterhapticsVRCOSCParameter.ContactHitLeft:
                {
                    float intensity = GetSettingValue<float>(InterhapticsVRCOSCSetting.Intensity);
                    float scaled = parameter.GetValue<float>() * intensity;
                    LogDebug($"[HIT LEFT] OSC={parameter.GetValue<float>()} → INTENSITY={scaled}");
                    //WyvrnAPI.CoreSetEventName("Haptic_Left");
                    WyvrnAPI.CoreSetEventName("RazerKrakenV4Pro_Steady");
                    break;
                }
            case InterhapticsVRCOSCParameter.ContactHitCenter:
                {
                    float intensity = GetSettingValue<float>(InterhapticsVRCOSCSetting.Intensity);
                    float scaled = parameter.GetValue<float>() * intensity;
                    LogDebug($"[HIT CENTER] OSC={parameter.GetValue<float>()} → INTENSITY={scaled}");
                    //WyvrnAPI.CoreSetEventName("Haptic_Center");
                    WyvrnAPI.CoreSetEventName("RazerKrakenV4Pro_Steady");
                    break;
                }
        }
    }
    #endregion
    
    #region Module Variables
    private enum InterhapticsVRCOSCSetting // For the settings UI, defines the order/grouping of what setting goes under what section
    {
        Mode, Intensity
    }
    private enum InterhapticsVRCOSCParameter
    {
        ContactHitRight, ContactHitLeft, ContactHitCenter
    }

    private enum InterhapticsVRCOSCVariable
    {
        ContactThreshold
    }
    #endregion
}