using Slacek.Client.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Slacek.Client.Desktop
{
    public class ChatPageViewModel : BaseViewModel
    {
        private readonly DataManager _dataManager;
        private Group _selectedGroup;

        public User LoggedInUser => _dataManager.LoggedInUser;

        public ObservableCollection<Group> Groups => _dataManager.Groups;

        public ObservableCollection<Message> MessagesInGroup => SelectedGroup is null ? null : _dataManager.Messages[SelectedGroup.GroupId];

        public Group SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _dataManager.GetUsers(value.GroupId);
                _dataManager.GetMessages(value.GroupId);
                OnPropertyChanged();
                SetProperty(ref _selectedGroup, value);
            }
        }

        public bool IsGroupSelected => SelectedGroup is not null;

        public ICommand SendMessageCommand { get; }

        public ICommand SeeOtherGroups { get; }

        public string NewMessageContent { get; set; }

        public bool IsMessageEmpty => string.IsNullOrWhiteSpace(NewMessageContent);

        public ChatPageViewModel(DataManager dataManager)
        {
            _dataManager = dataManager;
            SendMessageCommand = new RelayCommand(() =>
            {
                bool sent = _dataManager.SendNewMessage(SelectedGroup, NewMessageContent);
                if (sent)
                {
                    NewMessageContent = "";
                }
            }, () => IsGroupSelected && !IsMessageEmpty);
            SeeOtherGroups = new RelayCommand(() =>
            {
                Window window = new CreateOrJoinGroupWindow
                {
                    ShowInTaskbar = false
                };
                _ = window.ShowDialog();
            });
            try
            {
                _dataManager.GetGroups();
            }
            catch (Exception) { }
        }

        public ChatPageViewModel()
            : this(ServiceProvider.GetRequiredService<DataManager>())
        {
        }
    }
}
