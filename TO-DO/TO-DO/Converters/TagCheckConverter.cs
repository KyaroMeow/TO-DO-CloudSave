using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace TO_DO.Converters
{
	public class TagCheckConverter : IMultiValueConverter
	{
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not ObservableCollection<int> selectedIds || values[1] is not int tagId)
                return false;

            return selectedIds.Contains(tagId);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Не нужно реализовывать, управление через VM
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
