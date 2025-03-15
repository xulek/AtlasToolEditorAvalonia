using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

namespace AtlasToolEditorAvalonia
{
    public partial class InputDialog : Window
    {
        public string DialogTitle { get; set; }
        public string PromptText { get; set; }

        public InputDialog(string dialogTitle, string promptText)
        {
            // Set properties
            DialogTitle = dialogTitle;
            PromptText = promptText;

            InitializeComponent();

            // Set DataContext to self
            DataContext = this;
            // When window is opened, set focus to the input TextBox
            this.Opened += (sender, args) =>
            {
                var inputBox = this.FindControl<TextBox>("InputBox");
                inputBox.Focus();
            };

            // Register key down event for the input TextBox to handle Enter key
            var textBox = this.FindControl<TextBox>("InputBox");
            textBox.KeyDown += InputBox_KeyDown;
        }

        private void InputBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check if Enter key is pressed and no modifier keys are held
            if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
            {
                Ok_Click(sender, e);
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = this.FindControl<TextBox>("InputBox");
            Close(inputBox.Text);
        }
    }
}
