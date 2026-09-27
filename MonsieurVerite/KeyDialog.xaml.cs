using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MonsieurVerite;

public partial class KeyDialog : DialogWindow
{
    private readonly bool streamCipher;

    public KeyDialog(string fileName, bool streamCipher)
    {
        InitializeComponent();
        this.streamCipher = streamCipher;
        Heading.Text = streamCipher ? "Decryption keys" : "Decryption key";
        Caption.Text = $"Apply your own key to {fileName}.";
        KeyLabel.Text = streamCipher ? "Audio key" : "Video key";
        AesKeyPanel.Visibility = streamCipher ? Visibility.Visible : Visibility.Collapsed;
    }

    public string Key { get; private set; } = "";

    private void KeyBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var key = KeyBox.Text.Trim();
        var valid = ulong.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture,
            out var value);
        if (streamCipher)
        {
            var aesKey = AesKeyBox.Text.Trim();
            key = $"{key}:{aesKey}";
            valid = valid && value < 1UL << 56 && aesKey.Length == 32 &&
                    aesKey.All(char.IsAsciiHexDigit);
        }

        Key = key;
        OkButton.IsEnabled = valid;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
