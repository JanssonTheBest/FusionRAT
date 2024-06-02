using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Server.UI.Style.Common.CommonHelper
{
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(TextBoxHelper), new PropertyMetadata(default(string), OnPlaceholderChanged));

        public static readonly DependencyProperty UseCharProperty =
            DependencyProperty.RegisterAttached("UseChar", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(true, OnUseCharChanged));

        public static readonly DependencyProperty CharLimitProperty =
            DependencyProperty.RegisterAttached("CharLimit", typeof(int), typeof(TextBoxHelper), new PropertyMetadata(default(int), OnCharLimitChanged));

        public static readonly DependencyProperty ValueLimitProperty =
            DependencyProperty.RegisterAttached("ValueLimit", typeof(int), typeof(TextBoxHelper), new PropertyMetadata(default(int), OnValueLimitChanged));

        public static string GetPlaceholder(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderProperty);
        }

        public static void SetPlaceholder(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderProperty, value);
        }

        public static bool GetUseChar(DependencyObject obj)
        {
            return (bool)obj.GetValue(UseCharProperty);
        }

        public static void SetUseChar(DependencyObject obj, bool value)
        {
            obj.SetValue(UseCharProperty, value);
        }

        public static int GetCharLimit(DependencyObject obj)
        {
            return (int)obj.GetValue(CharLimitProperty);
        }

        public static void SetCharLimit(DependencyObject obj, int value)
        {
            obj.SetValue(CharLimitProperty, value);
        }

        public static int GetValueLimit(DependencyObject obj)
        {
            return (int)obj.GetValue(ValueLimitProperty);
        }

        public static void SetValueLimit(DependencyObject obj, int value)
        {
            obj.SetValue(ValueLimitProperty, value);
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.Loaded -= TextBoxOnLoaded;
                textBox.Loaded += TextBoxOnLoaded;

                textBox.GotFocus -= RemovePlaceholder;
                textBox.GotFocus += RemovePlaceholder;

                textBox.LostFocus -= ShowPlaceholder;
                textBox.LostFocus += ShowPlaceholder;

                ShowPlaceholder(textBox, null);
            }
        }

        private static void OnUseCharChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.PreviewTextInput -= TextBoxOnPreviewTextInput;
                if (!(bool)e.NewValue)
                {
                    textBox.PreviewTextInput += TextBoxOnPreviewTextInput;
                }

                DataObject.RemovePastingHandler(textBox, TextBoxPastingHandler);
                if (!(bool)e.NewValue)
                {
                    DataObject.AddPastingHandler(textBox, TextBoxPastingHandler);
                }
            }
        }

        private static void OnCharLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.PreviewTextInput -= OnPreviewTextInputForCharLimit;
                if ((int)e.NewValue > 0)
                {
                    textBox.PreviewTextInput += OnPreviewTextInputForCharLimit;
                }
            }
        }

        private static void OnValueLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.PreviewTextInput -= OnPreviewTextInputForValueLimit;
                if ((int)e.NewValue > 0)
                {
                    textBox.PreviewTextInput += OnPreviewTextInputForValueLimit;
                }
            }
        }

        private static void TextBoxOnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            ShowPlaceholder(textBox, null);
        }

        private static void RemovePlaceholder(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox.Text == GetPlaceholder(textBox))
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.White; // Set the foreground to white or any other color you prefer
            }
        }

        private static void ShowPlaceholder(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = GetPlaceholder(textBox);
                textBox.Foreground = Brushes.Gray; // Set the foreground to gray or any other color you prefer
                textBox.Opacity = 0.3;
            }
        }

        private static void TextBoxOnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static void TextBoxPastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var text = e.DataObject.GetData(DataFormats.Text) as string;
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsTextAllowed(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static void OnPreviewTextInputForCharLimit(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            int charLimit = GetCharLimit(textBox);

            if (textBox.SelectionLength > 0)
            {
                int newLength = textBox.Text.Length - textBox.SelectionLength + e.Text.Length;
                if (newLength > charLimit)
                {
                    e.Handled = true;
                }
            }
            else if (textBox.Text.Length + e.Text.Length > charLimit)
            {
                e.Handled = true;
            }
        }

        private static void OnPreviewTextInputForValueLimit(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            int valueLimit = GetValueLimit(textBox);
            string newText;

            if (textBox.SelectionLength > 0)
            {
                newText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength).Insert(textBox.SelectionStart, e.Text);
            }
            else
            {
                newText = textBox.Text + e.Text;
            }

            if (int.TryParse(newText, out int result))
            {
                if (result > valueLimit)
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }
    }
}
