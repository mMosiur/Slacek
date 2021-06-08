using Slacek.Client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Slacek.Client.Desktop
{
    internal class CreateOrJoinGroupWindowViewModel : BaseViewModel
    {
        private readonly ConnectionService _connectionService;

        public string CreateGroupName { get; set; }
        public ICommand CreateGroupCommand { get; }
        public bool CanCreateGroup => !(string.IsNullOrWhiteSpace(CreateGroupName) || WaitingForReply);

        public string JoinGroupName { get; set; }
        public ICommand JoinGroupCommand { get; }
        public bool CanJoinGroup => !(string.IsNullOrWhiteSpace(JoinGroupName) || WaitingForReply);

        public bool WaitingForReply { get; private set; } = false;


        public CreateOrJoinGroupWindowViewModel(ConnectionService connectionService)
        {

            _connectionService = connectionService;
            CreateGroupCommand = new RelayCommand(() =>
            {
                App.Current.Dispatcher.Invoke(delegate
                {
                    _connectionService.CreateNewGroup(CreateGroupName);
                    WaitingForReply = true;
                    void handler(object sender, NewGroupReceivedEventArgs e)
                    {
                        MessageBox.Show("Nowa grupa została utworzona");
                        WaitingForReply = false;
                        _connectionService.NewGroupReceived -= handler;
                    }
                    _connectionService.NewGroupReceived += handler;
                });
            });
            JoinGroupCommand = new RelayCommand(() =>
            {
                App.Current.Dispatcher.Invoke(delegate
                {
                    _connectionService.JoinGroup(JoinGroupName);
                    WaitingForReply = true;
                    void handler(object sender, NewGroupReceivedEventArgs e)
                    {
                        MessageBox.Show("Dołączono do grupy");
                        WaitingForReply = false;
                        _connectionService.NewGroupReceived -= handler;
                    }
                    _connectionService.NewGroupReceived += handler;
                });
            });
        }

        public CreateOrJoinGroupWindowViewModel()
            : this(ServiceProvider.GetRequiredService<ConnectionService>())
        {
        }
    }
}
