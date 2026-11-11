using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VRCOSC.App.SDK.Modules;
using VRCOSC.App.SDK.Parameters;
using VRCOSC.App.SDK.VRChat;

namespace RyciaVRCOSC.BoopCounter;

[ModuleTitle("Boop Counter")]
[ModuleDescription("Counts boops from the ValueFactory prefab and displays them in the VRChat ChatBox.")]
[ModuleType(ModuleType.Generic)]
public sealed class BoopCounterModule : Module
{
    private enum Setting { DataPath, Messages, Display, SpecialNumbers, ResetTime, MessageDuration }
    private enum Parameter { Boop }
    private enum DisplayMode { Daily, Lifetime, Both }
    private enum ChatState { Idle }
    private enum ChatEvent { Booped }
    private enum ChatVariable { Message }

    private static readonly TimeSpan messageInterval = TimeSpan.FromSeconds(2);
    private readonly object gate = new();
    private BoopStore? store;
    private BoopData data = new();
    private TimeSpan resetTime;
    private bool lastBoop;
    private bool pendingMessage;
    private bool dirty;
    private bool saveErrorLogged;
    private long? lastMessage;
    private string? pendingSpecial;

    protected override void OnPreLoad()
    {
        CreateTextBox(Setting.DataPath, "boops.json",
            "Where your boop data is stored, compatible with ValueFactory's Boop Counter's .json format if you are switching over.",
            "");
        CreateGroup("File Locations", Setting.DataPath);

        CreateToggle(Setting.Messages, "Enable messages", "Show a ChatBox message when you are booped.", true);
        CreateDropdown(Setting.Display, "Count display", "Choose which counts appear in the ChatBox.", DisplayMode.Both);
        CreateToggle(Setting.SpecialNumbers, "Special-number messages", "Celebrate special numbers in the displayed counts.", true);
        CreateTextBox(Setting.ResetTime, "Reset Daily Boops at", "Local time in 24-hour HH:mm or HH:mm:ss format.", "12:00:00");
        CreateSlider(Setting.MessageDuration, "Hide chat box after seconds",
            "Default duration for new Booped events on the ChatBox timeline. Edit existing events on the timeline.", 4, 1, 60);
        CreateGroup("Features", Setting.Messages, Setting.Display, Setting.SpecialNumbers, Setting.ResetTime, Setting.MessageDuration);

        RegisterParameter<bool>(Parameter.Boop, "OSCBoop", ParameterMode.Read,
            "Boop", "The Boolean parameter sent by the ValueFactory prefab.");
        SetRuntimeView(typeof(BoopCounterView));
    }

    protected override void OnPostLoad()
    {
        var message = CreateVariable<string>(ChatVariable.Message, "Boop message")!;
        CreateState(ChatState.Idle, "Default");
        CreateEvent(ChatEvent.Booped, "Booped", "{0}", [message],
            defaultLength: Math.Clamp(GetSettingValue<int>(Setting.MessageDuration), 1, 60));
    }

    protected override Task<bool> OnModuleStart()
    {
        lock (gate)
        {
            var time = GetSettingValue<string>(Setting.ResetTime)?.Trim();
            if (!TimeSpan.TryParseExact(time, [@"hh\:mm", @"hh\:mm\:ss"], CultureInfo.InvariantCulture, out resetTime))
            {
                Log("Set Reset Daily Boops at to a valid 24-hour time, such as 12:00:00.");
                return Task.FromResult(false);
            }

            try
            {
                var configured = GetSettingValue<string>(Setting.DataPath)?.Trim();
                var path = string.IsNullOrWhiteSpace(configured)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "VRCOSC", "RyciaVRCOSC", "BoopCounter", "boops.json")
                    : Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured));
                var nextStore = new BoopStore(path);
                var now = DateTimeOffset.Now;
                var loaded = nextStore.Load(now);
                data = loaded.RollOver(now, resetTime, TimeZoneInfo.Local);
                if (!nextStore.Exists || data != loaded)
                    nextStore.Save(data);
                store = nextStore;
            }
            catch (Exception e) when (IsFileError(e))
            {
                Log($"Could not open boops.json. {e.Message}");
                return Task.FromResult(false);
            }

            lastBoop = pendingMessage = dirty = saveErrorLogged = false;
            lastMessage = null;
            pendingSpecial = null;
            ChangeState(ChatState.Idle);
            return Task.FromResult(true);
        }
    }

    protected override Task OnModuleStop()
    {
        lock (gate)
        {
            SaveIfDirty();
            store = null;
        }
        return Task.CompletedTask;
    }

    protected override void OnAvatarChange(AvatarConfig? avatarConfig)
    {
        lock (gate)
            lastBoop = false;
    }

    protected override void OnRegisteredParameterReceived(RegisteredParameter parameter)
    {
        lock (gate)
        {
            var value = parameter.GetValue<bool>();
            var risingEdge = value && !lastBoop;
            lastBoop = value;
            if (store is null || !risingEdge)
                return;

            RollOver(DateTimeOffset.Now);
            var daily = checked(data.Daily + 1);
            data = data with
            {
                Daily = daily,
                Lifetime = checked(data.Lifetime + 1),
                Watermark = Math.Max(data.Watermark, daily)
            };
            dirty = true;

            if (GetSettingValue<bool>(Setting.Messages))
            {
                pendingMessage = true;
                if (GetSettingValue<bool>(Setting.SpecialNumbers))
                    pendingSpecial = GetSpecialMessage() ?? pendingSpecial;
            }
        }
    }

    [ModuleUpdate(ModuleUpdateMode.Custom, deltaMilliseconds: 250)]
    private void UpdateCounter()
    {
        lock (gate)
        {
            if (store is null)
                return;

            RollOver(DateTimeOffset.Now);
            SaveIfDirty();

            if (!GetSettingValue<bool>(Setting.Messages))
                ClearPendingMessage();
            if (!pendingMessage || (lastMessage.HasValue && Stopwatch.GetElapsedTime(lastMessage.Value) < messageInterval))
                return;

            var message = GetSettingValue<DisplayMode>(Setting.Display) switch
            {
                DisplayMode.Daily => $"Today: {data.Daily:N0} boops",
                DisplayMode.Lifetime => $"Total Boops: {data.Lifetime:N0}",
                _ => $"Today: {data.Daily:N0}\nTotal Boops: {data.Lifetime:N0}"
            };
            if (GetSettingValue<bool>(Setting.SpecialNumbers) && pendingSpecial is not null)
                message += $"\n{pendingSpecial}";

            SetVariableValue(ChatVariable.Message, message);
            TriggerEvent(ChatEvent.Booped);
            lastMessage = Stopwatch.GetTimestamp();
            ClearPendingMessage();
        }
    }

    internal void ResetDaily(bool undo)
    {
        lock (gate)
        {
            if (store is null)
                return;

            var now = DateTimeOffset.Now;
            RollOver(now);
            if (undo)
            {
                data = data with { Daily = data.Yesterday, Watermark = Math.Max(data.Watermark, data.Yesterday) };
            }
            else
            {
                data = data with { Daily = 0, Yesterday = data.Daily, LastReset = now.ToUnixTimeSeconds() };
            }

            ClearPendingMessage();
            dirty = true;
            SaveIfDirty();
        }
    }

    private void RollOver(DateTimeOffset now)
    {
        var updated = data.RollOver(now, resetTime, TimeZoneInfo.Local);
        if (updated == data)
            return;

        data = updated;
        dirty = true;
        ClearPendingMessage();
    }

    private void ClearPendingMessage()
    {
        pendingMessage = false;
        pendingSpecial = null;
    }

    private void SaveIfDirty()
    {
        if (!dirty || store is null)
            return;

        try
        {
            store.Save(data);
            dirty = saveErrorLogged = false;
        }
        catch (Exception e) when (IsFileError(e))
        {
            if (!saveErrorLogged)
                Log($"Could not save boops.json. {e.Message}");
            saveErrorLogged = true;
        }
    }

    private string? GetSpecialMessage()
    {
        var display = GetSettingValue<DisplayMode>(Setting.Display);
        var daily = display == DisplayMode.Lifetime ? null : SpecialMessage(data.Daily);
        return daily ?? (display == DisplayMode.Daily ? null : SpecialMessage(data.Lifetime));
    }

    private static string? SpecialMessage(long count)
    {
        if (count % 100 == 69)
            return "Nice";

        return (count % 1000) switch
        {
            404 => "Not found",
            420 => "blaze it",
            621 => "owo",
            _ when count % 10000 == 1337 => "leet",
            _ => null
        };
    }

    private static bool IsFileError(Exception e) =>
        e is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException;
}

public sealed class BoopCounterView : UserControl
{
    public BoopCounterView(Module module)
    {
        var counter = (BoopCounterModule)module;
        var panel = new StackPanel { Margin = new Thickness(12) };
        var reset = new Button { Content = "Reset Daily Boops", Margin = new Thickness(0, 0, 0, 8) };
        var undo = new Button { Content = "Undo Daily Boop Reset" };

        reset.Click += (_, _) =>
        {
            if (MessageBox.Show("Are you sure you want to reset today's boops to zero?",
                    "Reset Daily Boops", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
                counter.ResetDaily(undo: false);
        };
        undo.Click += (_, _) =>
        {
            if (MessageBox.Show("Are you sure you want to replace today's count with the previous daily count?",
                    "Undo Daily Boop Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
                counter.ResetDaily(undo: true);
        };

        panel.Children.Add(reset);
        panel.Children.Add(undo);
        Content = panel;
    }
}
