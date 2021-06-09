namespace Slacek.Client.Desktop
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : BasePage<LoginPageViewModel>
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.LoginCommand.Execute(PasswordBoxLogin);
        }

        private void RegisterButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.RegisterCommand.Execute(PasswordBoxRegister);
        }
    }
}
