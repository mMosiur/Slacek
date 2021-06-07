using Slacek.Client.Core;
using System.Windows.Input;

namespace Slacek.Client.Desktop
{
    public class LoginPageViewModel : BaseViewModel
    {
        private readonly ConnectionService _connectionService;
        private readonly DataManager _dataManager;
        public bool Authenticating { get; private set; }
        public bool CanLogin => !Authenticating;
        public bool CanRegister => !Authenticating;
        public bool IsLoginFocused { get; set; }
        public bool IsRegisterFocused { get; set; }
        public string LoginLogin { get; set; }
        public string LoginPassword { get; set; }
        public string LoginMessage { get; private set; }
        public string RegisterLogin { get; set; }
        public string RegisterUsername { get; set; }
        public string RegisterPassword { get; set; }
        public string RegisterMessage { get; private set; }
        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public LoginPageViewModel(ConnectionService connectionService, DataManager dataManager)
        {
            _connectionService = connectionService;
            _dataManager = dataManager;
            LoginCommand = new RelayCommand(() =>
            {
                if (Authenticating) return;
                LoginMessage = "Logowanie...";
                _connectionService.UserAuthentication += ConnectionManager_UserAuthentication;
                _connectionService.Authenticate(LoginLogin, LoginPassword);
                Authenticating = true;
            });
            RegisterCommand = new RelayCommand(() =>
            {
                if (Authenticating) return;
                RegisterMessage = "Rejestrowanie...";
                _connectionService.UserRegistration += ConnectionManager_UserRegistration;
                _connectionService.Register(RegisterLogin, RegisterUsername, RegisterPassword);
                Authenticating = true;
            });
        }

        public LoginPageViewModel()
            : this(ServiceProvider.GetRequiredService<ConnectionService>(),
                   ServiceProvider.GetRequiredService<DataManager>())
        {
        }

        private void ConnectionManager_UserRegistration(object sender, UserRegistrationEventArgs e)
        {
            try
            {
                if (e.Successful)
                {
                    RegisterMessage = "";
                    ServiceProvider.GetRequiredService<MainWindowViewModel>().CurrentPage = ApplicationPage.Chat;
                }
                else
                {
                    RegisterMessage = $"Rejestracja nie powiodła się";
                }
            }
            finally
            {
                _connectionService.UserRegistration -= ConnectionManager_UserRegistration;
                Authenticating = false;
            }
        }

        private void ConnectionManager_UserAuthentication(object sender, UserAuthenticationEventArgs e)
        {
            try
            {
                if (e.Successful)
                {
                    LoginMessage = "";
                    ServiceProvider.GetRequiredService<MainWindowViewModel>().CurrentPage = ApplicationPage.Chat;
                }
                else
                {
                    LoginMessage = $"Logowanie nie powiodło się";
                }
            }
            finally
            {
                _connectionService.UserAuthentication -= ConnectionManager_UserAuthentication;
                Authenticating = false;
            }
        }
    }
}
