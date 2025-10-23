using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TwitchWatcher.WPF.Behaviors
{
    public static class DataGridBehaviors
    {
        public static readonly DependencyProperty DeselectOnEmptyClickProperty =
            DependencyProperty.RegisterAttached(
                "DeselectOnEmptyClick",
                typeof(bool),
                typeof(DataGridBehaviors),
                new PropertyMetadata(false, OnDeselectOnEmptyClickChanged));

        public static void SetDeselectOnEmptyClick(DependencyObject element, bool value) =>
            element.SetValue(DeselectOnEmptyClickProperty, value);

        public static bool GetDeselectOnEmptyClick(DependencyObject element) =>
            (bool)element.GetValue(DeselectOnEmptyClickProperty);

        private static void OnDeselectOnEmptyClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid) return;

            if ((bool)e.NewValue)
                grid.PreviewMouseLeftButtonDown += GridOnPreviewMouseLeftButtonDown;
            else
                grid.PreviewMouseLeftButtonDown -= GridOnPreviewMouseLeftButtonDown;
        }

        private static void GridOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid) return;

            // Walk up the visual tree from the original source
            DependencyObject? current = e.OriginalSource as DependencyObject;

            while (current != null && current != grid)
            {
                if (current is DataGridRow || current is DataGridCell)
                    return; // Clicked a row/cell → keep selection
                current = VisualTreeHelper.GetParent(current);
            }

            // Clicked the grid background → clear selection
            grid.SelectedItem = null;
            grid.UnselectAll();
        }
    }
}
