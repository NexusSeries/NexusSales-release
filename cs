using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.IO;
using System.Collections;
using System.Windows.Data;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;

namespace NexusSales.FrontEnd.Pages.UserControls
{
    public partial class ExtractDataView : UserControl
    {
        private const string ColumnWidthsKey = "ExtractDataView_ColumnWidths";

        private static readonly DataGridLength[] DefaultColumnWidths = new DataGridLength[]
        {
            new DataGridLength(40),         // #
            new DataGridLength(1, DataGridLengthUnitType.Auto), // Craft
            new DataGridLength(1, DataGridLengthUnitType.Star), // First name
            new DataGridLength(1, DataGridLengthUnitType.Star), // Last name
            new DataGridLength(1, DataGridLengthUnitType.Star), // Referred by
            new DataGridLength(80),         // EE
            new DataGridLength(90),         // Shift
            new DataGridLength(130),        // Phone
            new DataGridLength(100),        // Need by
            new DataGridLength(180),        // Notes
        };

        public static List<ExtractDataView> ActiveViews { get; } = new List<ExtractDataView>();

        private ListCollectionView _collectionView;

        private string _currentFilterColumn;
        private string _currentFilterValue;
        private string _currentFilterCondition; // e.g., "Contains", "Equals", etc.

        public ObservableCollection<Requestee> RequesteeList { get; set; } = new ObservableCollection<Requestee>();

        public ExtractDataView()
        {
            InitializeComponent();
            this.Loaded += ExtractDataView_Loaded;
            this.Unloaded += ExtractDataView_Unloaded;

            // Add a sample row for testing
            RequesteeList.Add(new Requestee
            {
                RowNumber = 1,
                Craft = "Electrician",
                FirstName = "John",
                LastName = "Doe",
                ReferredBy = "Jane Smith",
                EE = "12345",
                Shift = "Morning",
                Phone = "555-1234",
                NeedBy = "2025-08-12",
                Notes = "Sample row for testing"
            });
        }
        private void ExtractDataView_Loaded(object sender, RoutedEventArgs e)
        {
            // Add a sample row for testing (do this before any view setup)
            var vm = this.DataContext as dynamic;
            if (vm != null && vm.RequesteeList != null && vm.RequesteeList.Count == 0)
            {
                vm.RequesteeList.Add(new Requestee
                {
                    RowNumber = 1,
                    Craft = "Electrician",
                    FirstName = "John",
                    LastName = "Doe",
                    ReferredBy = "Jane Smith",
                    EE = "12345",
                    Shift = "Morning",
                    Phone = "555-1234",
                    NeedBy = "2025-08-12",
                    Notes = "Sample row for testing"
                });
            }

            var saved = LoadColumnWidths();
            if (!string.IsNullOrEmpty(saved))
            {
                var widths = saved.Split(',').Select(s => double.TryParse(s, out var w) ? w : double.NaN).ToArray();
                for (int i = 0; i < widths.Length && i < AnimatedDataGrid.Columns.Count; i++)
                {
                    if (!double.IsNaN(widths[i]) && widths[i] > 0)
                        AnimatedDataGrid.Columns[i].Width = widths[i];
                }
            }

            // Subscribe to width changes for each column
            foreach (var col in AnimatedDataGrid.Columns)
            {
                DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn))
                    .AddValueChanged(col, DataGridColumn_WidthChanged);
            }

            foreach (var col in AnimatedDataGrid.Columns)
            {
                DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn))
                    .AddValueChanged(col, DataGridColumn_WidthChanged);
            }

            // Only set up the collection view if not already set
            if (_collectionView == null)
            {
                var source = AnimatedDataGrid.ItemsSource as IList;
                if (source == null && AnimatedDataGrid.ItemsSource is IEnumerable enumerable)
                    source = enumerable.Cast<object>().ToList();

                if (source != null)
                {
                    _collectionView = new ListCollectionView(source);
                    AnimatedDataGrid.ItemsSource = _collectionView;
                }
            }
        }

        private void ExtractDataView_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveColumnWidths();
            ActiveViews.Remove(this);
        }

        private string GetWidthsFilePath()
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(dir, "NexusSales");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, "ExtractDataView_ColumnWidths.txt");
        }

        private void DataGridColumn_WidthChanged(object sender, EventArgs e)
        {
            SaveColumnWidths();
        }

        private void SaveColumnWidths()
        {
            var widths = AnimatedDataGrid.Columns.Select(c => c.Width.DisplayValue.ToString()).ToArray();
            File.WriteAllText(GetWidthsFilePath(), string.Join(",", widths));
        }

        private string LoadColumnWidths()
        {
            var path = GetWidthsFilePath();
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private void AnimatedDataGrid_ColumnWidthChanged(object sender, DataGridColumnEventArgs e)
        {
            // Save all column widths as comma-separated string
            SaveColumnWidths();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // Example: Show a simple message or open your filter UI here
            Button btn = sender as Button;
            MessageBox.Show("Filter button clicked for column: " + (btn?.DataContext as DataGridColumnHeader)?.Content?.ToString());
        }

        private void ResetColumnWidths_Click(object sender, RoutedEventArgs e)
        {
            ResetColumnWidths();
        }

        public void ResetColumnWidths()
        {
            for (int i = 0; i < AnimatedDataGrid.Columns.Count && i < DefaultColumnWidths.Length; i++)
            {
                AnimatedDataGrid.Columns[i].Width = DefaultColumnWidths[i];
            }
            // Remove saved widths from file
            var path = GetWidthsFilePath();
            if (File.Exists(path)) File.Delete(path);
        }

        // Helper to get the clicked column
        private DataGridColumn GetContextColumn()
        {
            if (AnimatedDataGrid.CurrentCell.Column != null)
                return AnimatedDataGrid.CurrentCell.Column;
            return null;
        }

        private void TableContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            // Optionally, enable/disable menu items based on context
            // Example: disable Paste if clipboard is empty
            var menu = sender as ContextMenu;
            if (menu != null)
            {
                var pasteItem = menu.Items.OfType<MenuItem>().FirstOrDefault(i => (string)i.Header == "Paste cell");
                if (pasteItem != null)
                    pasteItem.IsEnabled = Clipboard.ContainsText();
            }
        }

        private void InsertColumnLeft_Click(object sender, RoutedEventArgs e)
        {
            // Implement logic to insert a column to the left
        }

        private void InsertColumnRight_Click(object sender, RoutedEventArgs e)
        {
            // Implement logic to insert a column to the right
        }

        private void ClearColumn_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
            {
                foreach (var item in AnimatedDataGrid.ItemsSource)
                {
                    var row = item as dynamic;
                    col.OnPastingCellClipboardContent(row, null);
                }
            }
        }

        private void ReadOnlyColumn_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.IsReadOnly = !col.IsReadOnly;
        }

        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.CellStyle = new Style(typeof(DataGridCell)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left) } };
        }

        private void AlignCenter_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.CellStyle = new Style(typeof(DataGridCell)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center) } };
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.CellStyle = new Style(typeof(DataGridCell)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) } };
        }

        private void AlignJustify_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.CellStyle = new Style(typeof(DataGridCell)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Justify) } };
        }

        private void AlignTop_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.CellStyle = new Style(typeof(DataGridCell)) { Setters = { new Setter(VerticalAlignmentProperty, VerticalAlignment.Top) } };
        }

        private void AlignMiddle_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.CellStyle = new Style(typeof(DataGridCell)) { Setters = { new Setter(VerticalAlignmentProperty, VerticalAlignment.Center) } };
        }

        private void AlignBottom_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.CellStyle = new Style(typeof(DataGridCell)) { Setters = { new Setter(VerticalAlignmentProperty, VerticalAlignment.Bottom) } };
        }

        private void FilterContains_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col == null) return;

            var binding = (col as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
            if (binding == null) return;

            _currentFilterColumn = binding.Path.Path;
            _currentFilterCondition = "Contains";
            _currentFilterValue = PromptForFilterValue($"Enter value to filter '{col.Header}' (contains):");
            if (string.IsNullOrEmpty(_currentFilterValue))
            {
                _collectionView.Filter = null;
                return;
            }

            _collectionView.Filter = item =>
            {
                var prop = item.GetType().GetProperty(_currentFilterColumn);
                if (prop == null) return true;
                var value = prop.GetValue(item, null)?.ToString();
                return value != null && value.IndexOf(_currentFilterValue, StringComparison.OrdinalIgnoreCase) >= 0;
            };
        }

        private void FilterNotContains_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col == null) return;

            var binding = (col as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
            if (binding == null) return;

            _currentFilterColumn = binding.Path.Path;
            _currentFilterCondition = "NotContains";
            _currentFilterValue = PromptForFilterValue($"Enter value to filter '{col.Header}' (does not contain):");
            if (string.IsNullOrEmpty(_currentFilterValue))
            {
                _collectionView.Filter = null;
                return;
            }

            _collectionView.Filter = item =>
            {
                var prop = item.GetType().GetProperty(_currentFilterColumn);
                if (prop == null) return true;
                var value = prop.GetValue(item, null)?.ToString();
                return value == null || value.IndexOf(_currentFilterValue, StringComparison.OrdinalIgnoreCase) < 0;
            };
        }

        private void FilterEquals_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col == null) return;

            var binding = (col as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
            if (binding == null) return;

            _currentFilterColumn = binding.Path.Path;
            _currentFilterCondition = "Equals";
            _currentFilterValue = PromptForFilterValue($"Enter value to filter '{col.Header}' (equals):");
            if (string.IsNullOrEmpty(_currentFilterValue))
            {
                _collectionView.Filter = null;
                return;
            }

            _collectionView.Filter = item =>
            {
                var prop = item.GetType().GetProperty(_currentFilterColumn);
                if (prop == null) return true;
                var value = prop.GetValue(item, null)?.ToString();
                return value != null && value.Equals(_currentFilterValue, StringComparison.OrdinalIgnoreCase);
            };
        }

        private void FilterNotEquals_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col == null) return;

            var binding = (col as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
            if (binding == null) return;

            _currentFilterColumn = binding.Path.Path;
            _currentFilterCondition = "NotEquals";
            _currentFilterValue = PromptForFilterValue($"Enter value to filter '{col.Header}' (not equals):");
            if (string.IsNullOrEmpty(_currentFilterValue))
            {
                _collectionView.Filter = null;
                return;
            }

            _collectionView.Filter = item =>
            {
                var prop = item.GetType().GetProperty(_currentFilterColumn);
                if (prop == null) return true;
                var value = prop.GetValue(item, null)?.ToString();
                return value == null || !value.Equals(_currentFilterValue, StringComparison.OrdinalIgnoreCase);
            };
        }

        private void FilterByValue_Click(object sender, RoutedEventArgs e)
        {
            // Show a dialog to select/filter by value
        }

        private void HideColumn_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            if (col != null)
                col.Visibility = Visibility.Collapsed;
        }

        private void ShowAllColumns_Click(object sender, RoutedEventArgs e)
        {
            foreach (var col in AnimatedDataGrid.Columns)
                col.Visibility = Visibility.Visible;
        }

        private void ResetColumnWidth_Click(object sender, RoutedEventArgs e)
        {
            var col = GetContextColumn();
            int idx = AnimatedDataGrid.Columns.IndexOf(col);
            if (col != null && idx >= 0 && idx < DefaultColumnWidths.Length)
                col.Width = DefaultColumnWidths[idx];
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            // Implement export logic (e.g., using CsvHelper or EPPlus)
            MessageBox.Show("Export to Excel not implemented yet.");
        }

        private void CopyCell_Click(object sender, RoutedEventArgs e)
        {
            if (AnimatedDataGrid.CurrentCell != null && AnimatedDataGrid.CurrentCell.Item != null && AnimatedDataGrid.CurrentCell.Column != null)
            {
                var binding = (AnimatedDataGrid.CurrentCell.Column as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
                if (binding != null)
                {
                    var propertyName = binding.Path.Path;
                    var value = AnimatedDataGrid.CurrentCell.Item.GetType().GetProperty(propertyName)?.GetValue(AnimatedDataGrid.CurrentCell.Item, null);
                    Clipboard.SetText(value?.ToString() ?? string.Empty);
                }
            }
        }

        private void PasteCell_Click(object sender, RoutedEventArgs e)
        {
            if (AnimatedDataGrid.CurrentCell != null && AnimatedDataGrid.CurrentCell.Item != null && AnimatedDataGrid.CurrentCell.Column != null)
            {
                var binding = (AnimatedDataGrid.CurrentCell.Column as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
                if (binding != null)
                {
                    var propertyName = binding.Path.Path;
                    var prop = AnimatedDataGrid.CurrentCell.Item.GetType().GetProperty(propertyName);
                    if (prop != null && prop.CanWrite)
                    {
                        var clipboardText = Clipboard.GetText();
                        prop.SetValue(AnimatedDataGrid.CurrentCell.Item, clipboardText);
                        AnimatedDataGrid.Items.Refresh();
                    }
                }
            }
        }

        private string PromptForFilterValue(string prompt)
        {
            return Microsoft.VisualBasic.Interaction.InputBox(prompt, "Filter", "");
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            _collectionView.Filter = null;
        }
    }
}

