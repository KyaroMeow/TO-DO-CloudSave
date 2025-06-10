using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace TO_DO.Converters
{
	public class TagCheckConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is ObservableCollection<int> selectedIds && parameter is int tagId)
				return selectedIds.Contains(tagId);
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool isChecked && parameter is int tagId && targetType == typeof(ObservableCollection<int>))
			{
				var collection = new ObservableCollection<int>();
				if (isChecked)
					collection.Add(tagId);
				else
					collection.Remove(tagId);
				return collection;
			}
			return Binding.DoNothing;
		}
	}
}
