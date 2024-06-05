using System.Windows;
using System.Windows.Controls;

namespace Server.UI.CustomControls.BuilderControls.BuilderCommon
{
    public partial class TopBorderUC : UserControl
    {
        public static readonly DependencyProperty ButtonImageSourceProperty =
            DependencyProperty.Register("ButtonImageSource", typeof(string), typeof(TopBorderUC), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register("IconSource", typeof(string), typeof(TopBorderUC), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TopBorderUC), new PropertyMetadata(default(string)));

        public string ButtonImageSource
        {
            get => (string)GetValue(ButtonImageSourceProperty);
            set => SetValue(ButtonImageSourceProperty, value);
        }

        public string IconSource
        {
            get => (string)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public TopBorderUC()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
