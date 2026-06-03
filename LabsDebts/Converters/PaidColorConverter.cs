using System.Globalization;

namespace LabsDebts.Converters;

public class PaidColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isPaid = (bool)value;

        return isPaid
            ? Colors.LimeGreen
            : Colors.Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null!;
    }
}
