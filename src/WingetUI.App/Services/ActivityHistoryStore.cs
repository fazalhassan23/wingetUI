using System.IO;
using System.Text.Json;
using WingetUI.App.Models;
using WingetUI.Core.Operations;

namespace WingetUI.App.Services;

public interface IActivityHistoryStore
{
    IReadOnlyList<ActivityEntry> Load();
    void Save(IEnumerable<ActivityEntry> entries);
}

public sealed class JsonActivityHistoryStore(string path) : IActivityHistoryStore
{
    private const int TerminalEntryLimit = 100;

    public IReadOnlyList<ActivityEntry> Load()
    {
        if (!File.Exists(path))
            return [];

        var entries = JsonSerializer.Deserialize<ActivityEntry[]>(File.ReadAllText(path)) ?? [];
        return entries
            .Where(entry => entry?.Package is not null)
            .OrderByDescending(entry => entry.Created)
            .ToArray()!;
    }

    public void Save(IEnumerable<ActivityEntry> entries)
    {
        var retainedEntries = entries.Where(entry => PackageOperationPolicy.IsPending(entry.Status))
            .Concat(entries.Where(entry => !PackageOperationPolicy.IsPending(entry.Status)).Take(TerminalEntryLimit));

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(retainedEntries));
        File.Move(temporaryPath, path, overwrite: true);
    }
}
