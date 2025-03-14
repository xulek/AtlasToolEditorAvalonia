using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AtlasToolEditorAvalonia
{
    public partial class InputDialog : Window
    {
        public string DialogTitle { get; set; }
        public string PromptText { get; set; }

        public InputDialog(string dialogTitle, string promptText)
        {
            DialogTitle = dialogTitle;
            PromptText = promptText;

            InitializeComponent();

            DataContext = this;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = this.FindControl<TextBox>("InputBox");
            Close(inputBox.Text);
        }
    }
}
