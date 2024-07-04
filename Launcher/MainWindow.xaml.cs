using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows;

namespace Launcher
{
    public partial class MainWindow : Window
    {
        private RadioButton nextRadioButton;
        private DispatcherTimer timer;
        private bool isAnimating;
        
        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            timer.Tick += Timer_Tick;

            rbtn1.Checked += RadioButton_Checked;
            rbtn1.Unchecked += RadioButton_Unchecked;
            rbtn2.Checked += RadioButton_Checked;
            rbtn2.Unchecked += RadioButton_Unchecked;
            rbtn3.Checked += RadioButton_Checked;
            rbtn3.Unchecked += RadioButton_Unchecked;
            rbtn4.Checked += RadioButton_Checked;
            rbtn4.Unchecked += RadioButton_Unchecked;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Start animation on window deactivate
            Storyboard storyboard1 = (Storyboard)this.Resources["WindowDeactivateStoryboard"];
            storyboard1.Begin();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Storyboard storyboard2 = (Storyboard)this.Resources["WindowActiveteStoryboard"];
            storyboard2.Begin();

            Storyboard storyboard1 = (Storyboard)this.Resources["WindowDeactivateStoryboard"];
            storyboard1.Stop();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            HandleRadioButtonChecked(sender as RadioButton);
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            HandleRadioButtonUnchecked(sender as RadioButton);
        }

        private void HandleRadioButtonChecked(RadioButton sender)
        {
            if (isAnimating || sender == null)
            {
                return;
            }

            if (sender == rbtn1)
                nextRadioButton = rbtn2;
            else if (sender == rbtn2)
                nextRadioButton = rbtn3;
            else if (sender == rbtn3)
                nextRadioButton = rbtn4;
            else if (sender == rbtn4)
                nextRadioButton = rbtn1;

            isAnimating = true;
            timer.Stop();
            timer.Start();
        }

        private void HandleRadioButtonUnchecked(RadioButton sender)
        {
            if (sender == null)
            {
                return;
            }

            if (sender.IsChecked == false)
            {
                timer.Stop();
                isAnimating = false;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            if (nextRadioButton != null)
            {
                nextRadioButton.IsChecked = true;
            }
            isAnimating = false;
        }
    }
}
