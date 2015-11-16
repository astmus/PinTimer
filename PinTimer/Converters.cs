using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PinTimer
{
	public class BoolToColorBrushConverter : IValueConverter
	{
		private static SolidColorBrush _enabledColor = new SolidColorBrush(Color.FromArgb(128,255,0,0));
		private static SolidColorBrush _disabledColor = new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]);
		static BoolToColorBrushConverter()
		{
			Color c = _disabledColor.Color;
			c.A = 200;
			c.B = (byte)(255 - c.B);
			c.G = (byte)(255 - c.G);
			c.R = (byte)(255 - c.R);
			_enabledColor = new SolidColorBrush(c);
		}


		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if ((bool)value)
				return _enabledColor;
			else
				return _disabledColor;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToStartStopImageImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new Uri(!(bool)value ? "Assets/timer.play.png" : "Assets/timer.stop.png", UriKind.Relative);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToPauseResumeImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new Uri(!(bool)value ? "Assets/timer.pause.png" : "Assets/timer.resume.png", UriKind.Relative);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToPinActionImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{			
			return new Uri( !(bool)value ? "Assets/pin.png" : "Assets/pin.remove.png", UriKind.Relative);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
