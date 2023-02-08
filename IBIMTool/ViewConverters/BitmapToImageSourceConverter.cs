using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;


namespace IBIMTool.ViewConverters
{
    public class BitmapToImageSourceConverter : IValueConverter
    {
        private static readonly Lazy<BitmapToImageSourceConverter> InstanceObj =
           new Lazy<BitmapToImageSourceConverter>(() => new BitmapToImageSourceConverter());

        public static BitmapToImageSourceConverter Instance => InstanceObj.Value;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Bitmap bmp = value as Bitmap;
            if (bmp == null)
            {
                if (parameter is Bitmap defaultBmp)
                {
                    return BitmapSourceConverter.ConvertFromImage(defaultBmp);
                }
            }

            return bmp == null ? null : BitmapSourceConverter.ConvertFromImage(bmp);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}