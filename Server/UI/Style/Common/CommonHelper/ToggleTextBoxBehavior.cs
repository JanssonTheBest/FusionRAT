using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Server.UI.Style.Common.CommonHelper
{
    public static class ToggleTextBoxBehavior
    {
        public static readonly DependencyProperty TargetTextBox1Property =
            DependencyProperty.RegisterAttached("TargetTextBox1", typeof(TextBox), typeof(ToggleTextBoxBehavior), new PropertyMetadata(null, OnTargetTextBox1Changed));

        public static readonly DependencyProperty TargetTextBox2Property =
            DependencyProperty.RegisterAttached("TargetTextBox2", typeof(TextBox), typeof(ToggleTextBoxBehavior), new PropertyMetadata(null, OnTargetTextBox2Changed));

        public static readonly DependencyProperty TargetBorderProperty =
            DependencyProperty.RegisterAttached("TargetBorder", typeof(Border), typeof(ToggleTextBoxBehavior), new PropertyMetadata(null, OnTargetBorderChanged));

        public static readonly DependencyProperty TargetComboBox1Property =
            DependencyProperty.RegisterAttached("TargetComboBox1", typeof(ComboBox), typeof(ToggleTextBoxBehavior), new PropertyMetadata(null, OnTargetComboBox1Changed));

        public static readonly DependencyProperty TargetComboBox2Property =
            DependencyProperty.RegisterAttached("TargetComboBox2", typeof(ComboBox), typeof(ToggleTextBoxBehavior), new PropertyMetadata(null, OnTargetComboBox2Changed));

        public static readonly DependencyProperty TargetButtonProperty =
            DependencyProperty.RegisterAttached("TargetButton", typeof(Button), typeof(ToggleTextBoxBehavior), new PropertyMetadata(null, OnTargetButtonChanged));

        public static TextBox GetTargetTextBox1(DependencyObject obj)
        {
            return (TextBox)obj.GetValue(TargetTextBox1Property);
        }

        public static void SetTargetTextBox1(DependencyObject obj, TextBox value)
        {
            obj.SetValue(TargetTextBox1Property, value);
        }

        public static TextBox GetTargetTextBox2(DependencyObject obj)
        {
            return (TextBox)obj.GetValue(TargetTextBox2Property);
        }

        public static void SetTargetTextBox2(DependencyObject obj, TextBox value)
        {
            obj.SetValue(TargetTextBox2Property, value);
        }

        public static Border GetTargetBorder(DependencyObject obj)
        {
            return (Border)obj.GetValue(TargetBorderProperty);
        }

        public static void SetTargetBorder(DependencyObject obj, Border value)
        {
            obj.SetValue(TargetBorderProperty, value);
        }

        public static ComboBox GetTargetComboBox1(DependencyObject obj)
        {
            return (ComboBox)obj.GetValue(TargetComboBox1Property);
        }

        public static void SetTargetComboBox1(DependencyObject obj, ComboBox value)
        {
            obj.SetValue(TargetComboBox1Property, value);
        }

        public static ComboBox GetTargetComboBox2(DependencyObject obj)
        {
            return (ComboBox)obj.GetValue(TargetComboBox2Property);
        }

        public static void SetTargetComboBox2(DependencyObject obj, ComboBox value)
        {
            obj.SetValue(TargetComboBox2Property, value);
        }

        public static Button GetTargetButton(DependencyObject obj)
        {
            return (Button)obj.GetValue(TargetButtonProperty);
        }

        public static void SetTargetButton(DependencyObject obj, Button value)
        {
            obj.SetValue(TargetButtonProperty, value);
        }

        private static void OnTargetTextBox1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton && e.NewValue is TextBox textBox)
            {
                toggleButton.Checked += (s, ev) =>
                {
                    textBox.IsReadOnly = false;
                    textBox.IsEnabled = true;
                };

                toggleButton.Unchecked += (s, ev) =>
                {
                    textBox.IsReadOnly = true;
                    textBox.IsEnabled = false;
                };
            }
        }

        private static void OnTargetTextBox2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton && e.NewValue is TextBox textBox)
            {
                toggleButton.Checked += (s, ev) =>
                {
                    textBox.IsReadOnly = false;
                    textBox.IsEnabled = true;
                };

                toggleButton.Unchecked += (s, ev) =>
                {
                    textBox.IsReadOnly = true;
                    textBox.IsEnabled = false;
                };
            }
        }

        private static void OnTargetBorderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton && e.NewValue is Border border)
            {
                toggleButton.Checked += (s, ev) => border.Opacity = 1.0;
                toggleButton.Unchecked += (s, ev) => border.Opacity = 0.6;
            }
        }

        private static void OnTargetComboBox1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton && e.NewValue is ComboBox comboBox)
            {
                toggleButton.Checked += (s, ev) =>
                {
                    comboBox.IsEnabled = true;
                };

                toggleButton.Unchecked += (s, ev) =>
                {
                    comboBox.IsEnabled = false;
                };
            }
        }

        private static void OnTargetComboBox2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton && e.NewValue is ComboBox comboBox)
            {
                toggleButton.Checked += (s, ev) =>
                {
                    comboBox.IsEnabled = true;
                };

                toggleButton.Unchecked += (s, ev) =>
                {
                    comboBox.IsEnabled = false;
                };
            }
        }

        private static void OnTargetButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton && e.NewValue is Button button)
            {
                toggleButton.Checked += (s, ev) => button.IsEnabled = true;
                toggleButton.Unchecked += (s, ev) => button.IsEnabled = false;
            }
        }
    }
}