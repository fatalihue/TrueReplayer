using Microsoft.UI.Xaml.Data;
using System;

namespace TrueReplayer.Converters
{
    public class NonNegativeIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue)
            {
                return intValue.ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string stringValue && int.TryParse(stringValue, out int result))
            {
                return Math.Max(0, result);
            }
            return 0;
        }
    }
}