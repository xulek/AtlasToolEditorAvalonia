using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace AtlasToolEditorAvalonia
{
    public partial class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }
        
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static async Task Show(Window owner, string message, string title)
        {
            var dlg = new MessageBox { Title = title };
            dlg.FindControl<TextBlock>("MessageText").Text = message;
            await dlg.ShowDialog(owner);
        }
    }
}
