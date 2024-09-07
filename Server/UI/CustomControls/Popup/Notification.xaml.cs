using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Server.UI.CustomControls.Popup
{
    /// <summary>
    /// Interaction logic for Notification.xaml
    /// </summary>
    public partial class Notification : Window
    {
        public Notification(string header, string content, string borderBrush, string imageSrc, string gradientStartColor, string gradientEndColor)
        {
            InitializeComponent();

            headerPop.Text = header;
            ContentPop.Text = content;
            borderBrushPop.BorderBrush = (Brush)new BrushConverter().ConvertFromString(borderBrush);

            BitmapImage bitmap = new BitmapImage(new Uri(imageSrc, UriKind.RelativeOrAbsolute));
            statusImgPop.Source = bitmap;

            LinearGradientBrush gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new Point(0.5, 1);
            gradientBrush.EndPoint = new Point(0, 0);
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(gradientStartColor), 0));
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(gradientEndColor), 1));

            statusBG.Background = gradientBrush;


        }
    }
}
