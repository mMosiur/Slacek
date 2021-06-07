namespace Slacek.Client.Desktop
{
    internal class MainWindowViewModel : BaseViewModel
    {
        private readonly DataManager _dataManager;
        private ApplicationPage _currentPage;
        public ApplicationPage CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }

        public MainWindowViewModel()
        {
            //CurrentPage = ApplicationPage.Chat;
            _dataManager = ServiceProvider.GetRequiredService<DataManager>();
        }
    }
}
