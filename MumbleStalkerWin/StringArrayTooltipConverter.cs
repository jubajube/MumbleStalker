using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace MumbleStalkerWin {

    [ValueConversion(typeof(string[]), typeof(string))]
    public class StringArrayTooltipConverter: MarkupExtension, IValueConverter {
        #region MarkupExtension

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return this;
        }

        #endregion

        #region IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return null;
            }
            var stringArray = (string[])value;
            var stringBuilder = new StringBuilder();
            foreach (var line in stringArray) {
                if (stringBuilder.Length > 0) {
                    stringBuilder.AppendLine();
                }
                stringBuilder.Append(line);
            }
            return stringBuilder.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

}
