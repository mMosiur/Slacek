using System.Windows.Input;

namespace Slacek.Client.Desktop
{
    /// <summary>
    /// Interaction logic for ChatPage.xaml
    /// </summary>
    public partial class ChatPage : BasePage<ChatPageViewModel>
    {
        public ChatPage()
        {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter)
            {
                ICommand command = ViewModel?.SendMessageCommand;
                if (command?.CanExecute(null) ?? false)
                {
                    command.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
