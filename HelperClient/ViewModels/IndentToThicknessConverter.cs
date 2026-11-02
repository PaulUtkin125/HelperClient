using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace HelperClient.ViewModels
{
    /// <summary>
    /// Отступ элемента дерева категорий (в DIP) в толщину левого поля Margin.
    /// Используется для визуального сохранения вложенной структуры справочника Category.
    /// </summary>
    public sealed class IndentToThicknessConverter : IValueConverter
    {
        public static readonly IndentToThicknessConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var indent = value is double d ? d : 0d;
            return new Thickness(indent, 2, 0, 2);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
