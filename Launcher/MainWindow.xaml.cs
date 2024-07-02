using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Launcher
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private bool isAnimating;
        private RadioButton nextRadioButton;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(10); // Set delay interval to 4 seconds
            timer.Tick += Timer_Tick;

            // Hook into Checked and Unchecked events for all RadioButtons
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
            // Stop animation on window activate
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
            // Reset timer and prepare for animation if another animation is not ongoing
            if (isAnimating || sender == null)
            {
                return;
            }

            // If there's a next RadioButton in sequence, prepare to activate it
            if (sender == rbtn1)
                nextRadioButton = rbtn2;
            else if (sender == rbtn2)
                nextRadioButton = rbtn3;
            else if (sender == rbtn3)
                nextRadioButton = rbtn4;
            else if (sender == rbtn4)
                nextRadioButton = rbtn1;

            // Start the timer to delay the activation of the next RadioButton
            isAnimating = true;
            timer.Stop();
            timer.Start();
        }

        private void HandleRadioButtonUnchecked(RadioButton sender)
        {
            // Stop timer and reset animation flag if RadioButton is unchecked
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
            // Timer tick event: stop timer and activate next RadioButton in sequence
            timer.Stop();
            if (nextRadioButton != null)
            {
                nextRadioButton.IsChecked = true;
            }
            isAnimating = false;
        }
    }
}
