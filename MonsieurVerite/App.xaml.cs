using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MonsieurVerite;

public partial class App
{
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public static string? Build { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion is { } text && text.IndexOf('+') is > 0 and var plus
            ? text[(plus + 1)..Math.Min(text.Length, plus + 8)]
            : null;

    protected override void OnStartup(StartupEventArgs e)
    {
        Strings.Use(CultureInfo.CurrentUICulture);
        base.OnStartup(e);
        Updater.WaitForPredecessor(e.Args);
        Updater.DeleteStaleFiles(AppContext.BaseDirectory);
        ThemeMode = ThemeMode.Dark;
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(BlurOnClickOutside));
    }

    private static void BlurOnClickOutside(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBoxBase box && sender is Window window)
        {
            var node = e.OriginalSource as DependencyObject;
            while (node is not null and not Visual)
            {
                node = LogicalTreeHelper.GetParent(node);
            }

            if (node is not Visual visual || (visual != box && !box.IsAncestorOf(visual)))
            {
                Blur(window);
            }
        }
    }

    public static void Blur(Window window)
    {
        FocusManager.SetFocusedElement(window, null);
        Keyboard.Focus(window);
    }
}
