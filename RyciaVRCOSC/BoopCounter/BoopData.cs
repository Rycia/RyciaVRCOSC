using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RyciaVRCOSC.BoopCounter;

internal sealed record BoopData
{
    [JsonRequired, JsonPropertyName("version")]
    public int Version { get; init; } = 2;

    [JsonRequired, JsonPropertyName("lifetime_boops")]
    public long Lifetime { get; init; }

    [JsonRequired, JsonPropertyName("daily_boops")]
    public long Daily { get; init; }

    [JsonRequired, JsonPropertyName("daily_boop_watermark")]
    public long Watermark { get; init; }

    [JsonRequired, JsonPropertyName("yesterdays_daily_boops")]
    public long Yesterday { get; init; }

    [JsonRequired, JsonPropertyName("last_reset_time")]
    public long LastReset { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; init; }

    public BoopData RollOver(DateTimeOffset now, TimeSpan resetTime, TimeZoneInfo zone)
    {
        var localDate = TimeZoneInfo.ConvertTime(now, zone).Date;
        var boundary = GetBoundary(localDate, resetTime, zone);
        if (boundary > now)
        {
            localDate = localDate.AddDays(-1);
            boundary = GetBoundary(localDate, resetTime, zone);
        }

        var lastReset = DateTimeOffset.FromUnixTimeSeconds(LastReset);
        if (lastReset >= boundary)
            return this;

        var previousBoundary = GetBoundary(localDate.AddDays(-1), resetTime, zone);
        return this with
        {
            Daily = 0,
            Watermark = Math.Max(Watermark, Daily),
            Yesterday = lastReset >= previousBoundary ? Daily : 0,
            LastReset = boundary.ToUnixTimeSeconds()
        };
    }

    private static DateTimeOffset GetBoundary(DateTime date, TimeSpan time, TimeZoneInfo zone)
    {
        var local = DateTime.SpecifyKind(date.Add(time), DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(local))
            local = local.AddSeconds(1);

        var offset = zone.GetUtcOffset(local);
        if (zone.IsAmbiguousTime(local))
        {
            var offsets = zone.GetAmbiguousTimeOffsets(local);
            offset = offsets[0] > offsets[1] ? offsets[0] : offsets[1];
        }

        return new DateTimeOffset(local, offset);
    }
}

internal sealed class BoopStore
{
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    private readonly string path;
    private string? savedJson;
    private bool preserveOriginal;

    public BoopStore(string path) => this.path = path;

    public BoopData Load(DateTimeOffset now)
    {
        try
        {
            savedJson = File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            return new BoopData { LastReset = now.ToUnixTimeSeconds() };
        }
        catch (DirectoryNotFoundException)
        {
            return new BoopData { LastReset = now.ToUnixTimeSeconds() };
        }

        var data = JsonSerializer.Deserialize<BoopData>(savedJson, jsonOptions);
        if (data is null || data.Version != 2 || data.Lifetime < 0 || data.Daily < 0 ||
            data.Watermark < 0 || data.Yesterday < 0 || data.LastReset < 0 ||
            data.LastReset > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            throw new InvalidDataException("Expected a valid ValueFactory version-2 counter file.");

        preserveOriginal = true;
        return data;
    }

    public bool Exists => savedJson is not null;

    public void Save(BoopData data)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        using var writeLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.Write,
            FileShare.None, 1, FileOptions.DeleteOnClose);

        if (Exists && File.ReadAllText(path) != savedJson)
            throw new IOException("The counter file changed on disk. Close the other writer and restart the module.");

        if (preserveOriginal)
        {
            var backup = path + ".pre-vrcosc.bak";
            if (!File.Exists(backup))
                File.Copy(path, backup, overwrite: false);
            preserveOriginal = false;
        }

        var json = JsonSerializer.Serialize(data, jsonOptions);
        var temporaryPath = Path.Combine(directory, $".boops-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, json);
            if (Exists)
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);

            savedJson = json;
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
