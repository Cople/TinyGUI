using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TinyGUI.Converters
{
    public class IsVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string parameterText = parameter as string;
            bool invert = parameterText == "Invert";
            bool isVisible;
            if (!string.IsNullOrEmpty(parameterText) && parameterText != "Invert")
            {
                isVisible = string.Equals(System.Convert.ToString(value, culture), parameterText, StringComparison.Ordinal);
            }
            else
            {
                isVisible = value is bool visible && visible;
            }

            if (invert)
            {
                isVisible = !isVisible;
            }

            if (isVisible)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
