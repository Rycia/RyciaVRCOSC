using System;
using BindingFlags = System.Reflection.BindingFlags;
using System.Threading.Tasks;
using VRCOSC.App.SDK.Modules;
using VRCOSC.App.SDK.Parameters;
using VRCOSC.App.SDK.VRChat;

namespace RyciaVRCOSC.BiteTech;

[ModuleTitle("BiteTech")]
[ModuleDescription("Connects Sacred's BiteTech prefab to VRChat's grab input.")]
[ModuleType(ModuleType.Generic)]
public sealed class BiteTechModule : Module
{
    private enum Setting { Hand, Debug }
    private enum Parameter { Grab, Reset }
    private enum GrabHand { Right, Left }

    private readonly object gate = new();
    private Action<string, object?[]>? sendOsc;
    private string grabAddress = string.Empty;
    private bool biting;

    protected override void OnPreLoad()
    {
        CreateDropdown(Setting.Hand, "Grab hand", "The original BiteTech prefab uses Right. Applied when the module starts.", GrabHand.Right);
        CreateToggle(Setting.Debug, "Diagnostic logging", "Log grab and release messages.", false);
        CreateGroup("Features", Setting.Hand, Setting.Debug);
        RegisterParameter<bool>(Parameter.Grab, "bite/Grab", ParameterMode.Read,
            "Bite grab", "The BiteTech prefab's Boolean grab signal.");
        RegisterParameter<bool>(Parameter.Reset, "bite/Reset", ParameterMode.Read,
            "Bite reset", "Every received reset message releases the grab, matching the companion app.");
    }

    protected override Task<bool> OnModuleStart()
    {
        lock (gate)
        {
            var player = GetPlayer();
            sendOsc = BindSharedSender(player);
            if (sendOsc is null)
            {
                Log("This version of VRCOSC does not expose the OSC sender BiteTech needs for held grab input.");
                return Task.FromResult(false);
            }

            var hand = GetSettingValue<GrabHand>(Setting.Hand);
            grabAddress = hand == GrabHand.Left ? "/input/GrabLeft" : "/input/GrabRight";
            biting = false;
            Send(false);
            return Task.FromResult(true);
        }
    }

    protected override void OnRegisteredParameterReceived(RegisteredParameter parameter)
    {
        lock (gate)
        {
            if (sendOsc is null)
                return;

            var reset = parameter.Lookup.Equals(Parameter.Reset);
            var next = !reset && parameter.GetValue<bool>();
            if (reset || next != biting)
                Send(next);
        }
    }

    protected override void OnAvatarChange(AvatarConfig? avatarConfig)
    {
        lock (gate)
        {
            if (sendOsc is not null)
                Send(false);
        }
    }

    protected override Task OnModuleStop()
    {
        lock (gate)
        {
            if (sendOsc is null)
                return Task.CompletedTask;

            try
            {
                Send(false);
            }
            finally
            {
                sendOsc = null;
                biting = false;
            }
        }
        return Task.CompletedTask;
    }

    private void Send(bool value)
    {
        sendOsc!(grabAddress, [value ? 1 : 0]);
        biting = value;
        if (GetSettingValue<bool>(Setting.Debug))
            Log(value ? "Biting" : "Released");
    }

    // Player.GrabLeft/Right send short pulses. Use the same sender to hold the input
    // until the prefab releases it. Resolve the private field once per module start.
    private static Action<string, object?[]>? BindSharedSender(Player player)
    {
        var client = typeof(Player).GetField("oscClient", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(player);
        var send = client?.GetType().GetMethod("Send", [typeof(string), typeof(object[])]);
        return send?.CreateDelegate<Action<string, object?[]>>(client);
    }
}
