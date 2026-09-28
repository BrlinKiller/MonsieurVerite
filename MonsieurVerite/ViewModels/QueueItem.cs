using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MonsieurVerite.ViewModels;

public enum KeyState
{
    Unknown,
    Present,
    Missing,
    Recovered,
}

public enum ItemStatus
{
    Pending,
    Queued,
    Running,
    Done,
    Skipped,
    Error,
    Cancelled,
}

public sealed partial class QueueItem : ObservableObject
{
    public QueueItem(string fullPath)
    {
        FullPath = fullPath;
        FileName = Path.GetFileName(fullPath);
        Size = new FileInfo(fullPath) is { Exists: true } info ? info.Length : 0;
        HasVsScript = true;
        Detail = "";
    }

    public string FullPath { get; }

    public string FileName { get; }

    public long Size { get; }

    public string SizeText => FormatSize(Size);

    public static string FormatSize(long bytes) => bytes switch
    {
        0 => "",
        < 1L << 20 => $"{Math.Max(1, bytes >> 10)} KB",
        < 1L << 30 => $"{bytes / (double)(1L << 20):0} MB",
        _ => $"{bytes / (double)(1L << 30):0.0} GB",
    };

    [ObservableProperty] public partial bool IsChecked { get; set; }

    [ObservableProperty] public partial KeyState Key { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VersionOrder))]
    public partial string? Version { get; set; }

    public System.Version? VersionOrder =>
        Version is null ? null :
        System.Version.TryParse(Version, out var parsed) ? parsed : new System.Version(0, 0);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSubtitles), nameof(SubtitlesTip))]
    public partial IReadOnlyList<string>? Subtitles { get; set; }

    public bool? HasSubtitles => Subtitles is null ? null : Subtitles.Count > 0;

    public string SubtitlesTip => Strings.CACHED_SUBTITLES_TIP(string.Join(", ", Subtitles ?? []));

    [ObservableProperty] public partial bool HasVsScript { get; set; }

    [ObservableProperty] public partial bool StreamCipher { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProgress), nameof(StatusLabel))]
    public partial ItemStatus Status { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    public partial string Detail { get; set; }

    public bool HasProgress =>
        Status is ItemStatus.Running or ItemStatus.Done or ItemStatus.Error or ItemStatus.Cancelled;

    public string StatusLabel => Status switch
    {
        ItemStatus.Running when Detail.Length > 0 => Detail,
        ItemStatus.Pending => Strings.STATUS_PENDING,
        ItemStatus.Queued => Strings.STATUS_QUEUED,
        ItemStatus.Running => Strings.STATUS_RUNNING,
        ItemStatus.Done => Strings.STATUS_DONE,
        ItemStatus.Skipped => Strings.STATUS_SKIPPED,
        ItemStatus.Error => Strings.STATUS_ERROR,
        ItemStatus.Cancelled => Strings.STATUS_CANCELLED,
        _ => "",
    };

    [ObservableProperty] public partial double Progress { get; set; }

    [ObservableProperty] public partial string? OutputPath { get; set; }

    [ObservableProperty] public partial ulong? VideoKey { get; set; }
}
