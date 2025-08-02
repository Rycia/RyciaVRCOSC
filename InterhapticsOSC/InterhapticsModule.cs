using VRCOSC.Core;
using VRCOSC.Data;

namespace InterhapticsOSC;

public class InterhapticsModule : ModuleBase
{
    public override string Name => "InterhapticsOSC";
    public override string Author => "Rycia";
    public override string Version => "1.0.0";

    public override void OSCMessageReceived(OSCMessageData data)
    {
        if (data.Address == "/avatar/parameters/HapticSignal")
        {
            float value = data.GetFloat();
            // TODO: Add call to Razer SDK or log
            Console.WriteLine($"[RazerSensa] Received value: {value}");
        }
    }
}
