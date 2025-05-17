using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TrueReplayer.Models;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.UI.Controls;

namespace TrueReplayer.Controllers
{
    public class DelayManager
    {
        private readonly TextBox delayTextBox;
        private readonly ObservableCollection<ActionItem> actions;
        private readonly DataGrid dataGrid;

        public DelayManager(TextBox delayTextBox, ObservableCollection<ActionItem> actions, DataGrid dataGrid)
        {
            this.delayTextBox = delayTextBox;
            this.actions = actions;
            this.dataGrid = dataGrid;
        }

        public void HandleKeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool isDigit = (e.Key >= Windows.System.VirtualKey.Number0 && e.Key <= Windows.System.VirtualKey.Number9) ||
                           (e.Key >= Windows.System.VirtualKey.NumberPad0 && e.Key <= Windows.System.VirtualKey.NumberPad9);

            bool isControlKey = e.Key == Windows.System.VirtualKey.Back ||
                                e.Key == Windows.System.VirtualKey.Delete ||
                                e.Key == Windows.System.VirtualKey.Left ||
                                e.Key == Windows.System.VirtualKey.Right ||
                                e.Key == Windows.System.VirtualKey.Enter;

            if (!isDigit && !isControlKey)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!int.TryParse(delayTextBox.Text, out int newDelay) || newDelay < 0)
                {
                    delayTextBox.Text = "100";
                    delayTextBox.SelectionStart = delayTextBox.Text.Length;
                    return;
                }

                var selectedItems = dataGrid.SelectedItems.Cast<ActionItem>().ToList();
                if (selectedItems.Any())
                {
                    foreach (var item in selectedItems)
                        item.Delay = newDelay;

                    dataGrid.ItemsSource = null;
                    dataGrid.ItemsSource = actions;

                    System.Diagnostics.Debug.WriteLine($"✔️ Delay alterado em lote para {newDelay}ms.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✔️ Delay padrão de gravação atualizado para {newDelay}ms.");
                }
            }
        }

        public void HandleTextChanging(object sender, TextBoxTextChangingEventArgs args)
        {
            if (sender is not TextBox textBox) return;

            string newText = textBox.Text;

            if (string.IsNullOrWhiteSpace(newText) || !newText.All(char.IsDigit))
            {
                textBox.Text = "100";
                textBox.SelectionStart = textBox.Text.Length;
                return;
            }

            if (!int.TryParse(newText, out int delay) || delay < 0)
            {
                textBox.Text = "100";
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
    }

}
