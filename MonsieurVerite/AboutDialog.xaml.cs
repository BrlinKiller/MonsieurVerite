using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using MonsieurVerite.ViewModels;

namespace MonsieurVerite;

public partial class AboutDialog : DialogWindow
{
    public AboutDialog(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();

        TitleText.Text = $"charlotte {App.Version}";
        BuildText.Text = App.Build ?? Strings.UNKNOWN_BUILD;
        EngineText.Text = viewModel.Engine is null ? Strings.NO_ENGINE_FOUND
            : viewModel.EngineVersion is { } version ? $"charlotte-cli {version}"
            : "charlotte-cli";

        AppendFormat(CreditsText, nameof(Strings.POWERED_BY),
            ("ffmpeg", Link("ffmpeg", "https://ffmpeg.org")),
            ("vapoursynth", Link("VapourSynth", "https://www.vapoursynth.com")),
            ("gi_cutscenes", Link("GI‑cutscenes", "https://github.com/ToaHartor/GI-cutscenes")),
            ("usm_diviner", Link("UsmDiviner", "https://github.com/Senkin219/UsmDiviner")));
        SponsorText.Inlines.Add(" ");
        AppendFormat(SponsorText, nameof(Strings.SPONSOR_PROMPT),
            ("link", Link(Strings.SPONSOR_LINK, "https://github.com/sponsors/lunarmint")));
    }

    private static Hyperlink Link(string text, string uri) =>
        new(new Run(text)) { NavigateUri = new Uri(uri) };

    // A translation may move the links, so the text is split around its placeholders rather than
    // written as runs in the xaml.
    private static void AppendFormat(
        TextBlock block, string key, params (string Name, Inline Link)[] links)
    {
        var parts = Strings.Placeholder().Split(Strings.Template(key));
        for (var i = 0; i < parts.Length; i++)
        {
            block.Inlines.Add(i % 2 == 0
                ? new Run(parts[i])
                : links.First(link => link.Name == parts[i]).Link);
        }
    }

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        e.Handled = true;
    }
}
