using NLog;
using NLog.Targets;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Server.UI.CustomControls.NLog
{
    [Target("WpfItemsControlTarget")]
    public sealed class WpfItemsControlTarget : TargetWithLayout
    {
        private readonly ObservableCollection<TextBlock> _logMessages;

        public WpfItemsControlTarget(ObservableCollection<TextBlock> logMessages)
        {
            _logMessages = logMessages;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var logMessage = Layout.Render(logEvent);
            var callSite = logEvent.CallerMemberName;
            var textBlock = new TextBlock
            {
                FontSize = 17,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.Medium
            };

            var levelColor = GetLevelColor(logEvent.Level);
            var messageColor = GetMessageColor(logEvent.Level);

            string levelName = logEvent.Level == LogLevel.Debug ? "Success" : logEvent.Level.ToString();

            textBlock.Inlines.Add(new Run($"[{logEvent.TimeStamp:HH:mm:ss}] {levelName}: ")
            {
                Foreground = levelColor
            });

            textBlock.Inlines.Add(new Run(logMessage)
            {
                Foreground = messageColor
            });

            textBlock.MouseEnter += (sender, args) =>
            {
                textBlock.Opacity = 0.6;
                textBlock.Cursor = Cursors.Hand;
            };

            textBlock.MouseLeave += (sender, args) =>
            {
                textBlock.Opacity = 1.0;
                textBlock.Cursor = Cursors.Arrow;
            };

            textBlock.TextWrapping = TextWrapping.Wrap;

            textBlock.MouseUp += (sender, args) =>
            {
                Clipboard.SetText($"[{logEvent.TimeStamp:HH:mm:ss}] {callSite} {levelName}: {logMessage}");
            };

            _logMessages.Add(textBlock);
        }

        private Brush GetLevelColor(LogLevel level)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(level.Name switch
            {
                "Debug" => "#28A745", // Success color
                "Info" => "#36DBFF",
                "Warn" => "#FFF736",
                "Error" => "#FF3636",
                "Fatal" => "#C82929",
                _ => "#808080",
            }));
        }

        private Brush GetMessageColor(LogLevel level)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(level.Name switch
            {
                "Debug" => "#155724", // Success color
                "Info" => "#3585AE",
                "Warn" => "#AE9335",
                "Error" => "#AE3535",
                "Fatal" => "#7A2727",
                _ => "#C0C0C0",
            }));
        }
    }
}