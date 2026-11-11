using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
    private enum Setting { Address, Port, Hand, Debug }
    private enum Parameter { Grab, Reset }
    private enum GrabHand { Right, Left }

    private readonly object gate = new();
    private UdpClient? client;
    private byte[] pressed = Array.Empty<byte>();
    private byte[] released = Array.Empty<byte>();
    private bool biting;

    protected override void OnPreLoad()
    {
        CreateTextBox(Setting.Address, "Output address", "VRChat's OSC destination IP address. Applied when the module starts.", "127.0.0.1");
        CreateTextBox(Setting.Port, "Output port", "VRChat's OSC input port, normally 9000. Applied when the module starts.", 9000);
        CreateGroup("Connection", Setting.Address, Setting.Port);
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
            var port = GetSettingValue<int>(Setting.Port);
            if (!IPAddress.TryParse(GetSettingValue<string>(Setting.Address), out var address) || port < 1 || port > 65535)
            {
                Log("Enter a valid output IP address and port from 1 to 65535.");
                return Task.FromResult(false);
            }

            try
            {
                var hand = GetSettingValue<GrabHand>(Setting.Hand);
                var path = hand == GrabHand.Left ? "/input/GrabLeft" : "/input/GrabRight";
                pressed = Encode(path, true);
                released = Encode(path, false);
                client = new UdpClient(address.AddressFamily);
                client.Connect(address, port);
                biting = false;
                Send(false);
                return Task.FromResult(true);
            }
            catch (Exception e) when (e is SocketException or ArgumentException)
            {
                client?.Dispose();
                client = null;
                Log($"Could not start BiteTech. {e.Message}");
                return Task.FromResult(false);
            }
        }
    }

    protected override void OnRegisteredParameterReceived(RegisteredParameter parameter)
    {
        lock (gate)
        {
            if (client is null)
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
            if (client is not null)
                Send(false);
        }
    }

    protected override Task OnModuleStop()
    {
        lock (gate)
        {
            if (client is null)
                return Task.CompletedTask;

            try
            {
                Send(false);
            }
            finally
            {
                client.Dispose();
                client = null;
                biting = false;
            }
        }
        return Task.CompletedTask;
    }

    private void Send(bool value)
    {
        try
        {
            var packet = value ? pressed : released;
            client!.Send(packet, packet.Length);
            biting = value;
            if (GetSettingValue<bool>(Setting.Debug))
                Log(value ? "Biting" : "Released");
        }
        catch (SocketException e)
        {
            Log($"Could not send BiteTech input. {e.Message}");
        }
    }

    // OSC strings are null-terminated and padded to four bytes. Boolean tags carry no payload.
    internal static byte[] Encode(string address, bool value)
    {
        var length = (Encoding.UTF8.GetByteCount(address) + 4) & ~3;
        var packet = new byte[length + 4];
        Encoding.UTF8.GetBytes(address, 0, address.Length, packet, 0);
        packet[length] = (byte)',';
        packet[length + 1] = (byte)(value ? 'T' : 'F');
        return packet;
    }
}
