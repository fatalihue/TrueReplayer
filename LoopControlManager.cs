using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Linq;

namespace TrueReplayer.Controllers
{
    public class LoopControlManager
    {
        private readonly TextBox loopCountTextBox;
        private readonly ToggleSwitch enableLoopSwitch;
        private readonly TextBox loopIntervalTextBox;
        private readonly ToggleSwitch loopIntervalSwitch;

        public LoopControlManager(TextBox loopCountTextBox, ToggleSwitch enableLoopSwitch, TextBox loopIntervalTextBox, ToggleSwitch loopIntervalSwitch)
        {
            this.loopCountTextBox = loopCountTextBox;
            this.enableLoopSwitch = enableLoopSwitch;
            this.loopIntervalTextBox = loopIntervalTextBox;
            this.loopIntervalSwitch = loopIntervalSwitch;
        }

        public void HandleLoopCountKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (int.TryParse(loopCountTextBox.Text, out int loopCount) && loopCount >= 0)
                {
                    enableLoopSwitch.IsOn = true;
                }
                else
                {
                    loopCountTextBox.Text = "0";
                    enableLoopSwitch.IsOn = false;
                }
            }
        }

        public void HandleLoopCountTextChanging(object sender, TextBoxTextChangingEventArgs args)
        {
            if (sender is not TextBox textBox) return;

            string newText = textBox.Text;
            if (string.IsNullOrEmpty(newText) || !newText.All(char.IsDigit) || int.Parse(newText) < 0)
            {
                string validText = new string(newText.Where(char.IsDigit).ToArray());
                textBox.Text = string.IsNullOrEmpty(validText) ? "0" : validText;
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        public void HandleLoopIntervalKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (int.TryParse(loopIntervalTextBox.Text, out int loopInterval) && loopInterval >= 0)
                {
                    loopIntervalSwitch.IsOn = true;
                }
                else
                {
                    loopIntervalTextBox.Text = "1000";
                    loopIntervalSwitch.IsOn = false;
                }
            }
        }

        public void HandleLoopIntervalTextChanging(object sender, TextBoxTextChangingEventArgs args)
        {
            if (sender is not TextBox textBox) return;

            string newText = textBox.Text;
            if (string.IsNullOrEmpty(newText) || !newText.All(char.IsDigit) || int.Parse(newText) < 0)
            {
                string validText = new string(newText.Where(char.IsDigit).ToArray());
                textBox.Text = string.IsNullOrEmpty(validText) ? "1000" : validText;
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
    }
}