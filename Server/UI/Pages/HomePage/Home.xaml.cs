using NLog;
using System.Windows;
using System.Windows.Controls;
using Server.UI.CustomControls.NLog;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Data;

namespace Server.UI.Pages.HomePage
{
    public partial class Home : UserControl
    {
        private static readonly Logger NLogLogger = LogManager.GetCurrentClassLogger();
        private static readonly CustomLogger Logger = new CustomLogger(NLogLogger);
        private readonly ObservableCollection<TextBlock> _logMessages = [];

        public Home()
        {
            InitializeComponent();
            LogsItemsControl.ItemsSource = _logMessages;
            ConfigureNLog();

            Logger.Success("This is a test success message.");
            Logger.Info("This is a test log message.");
            Logger.Warn("This is a test log message.");
            Logger.Error("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
            Logger.Fatal("This is a test log message.");
        }

        private void ConfigureNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logConsole = new WpfItemsControlTarget(_logMessages)
            {
                Layout = "${message}"
            };
            config.AddTarget("wpf", logConsole);
            config.LoggingRules.Add(new NLog.Config.LoggingRule("*", LogLevel.Debug, logConsole));

            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "file.txt",
                Layout = "${longdate}|${level}|${message}"
            };
            config.AddTarget("file", logFile);
            config.LoggingRules.Add(new NLog.Config.LoggingRule("*", LogLevel.Debug, logFile));

            LogManager.Configuration = config;
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            _logMessages.Clear();
        }

        private void InnerScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
    public class SubtractValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string stringParameter)
            {
                if (double.TryParse(stringParameter, out double subtrahend))
                {
                    return Math.Max(0, doubleValue - subtrahend);
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}