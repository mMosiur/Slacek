using System.Windows.Controls;

namespace Slacek.Client.Desktop
{
    public class BasePage<VM> : Page where VM : BaseViewModel, new()
    {
        private VM _viewModel;

        public VM ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (_viewModel == value)
                    return;
                _viewModel = value;
                DataContext = _viewModel;
            }
        }

        public BasePage(VM viewModel)
        {
            ViewModel = viewModel;
        }

        public BasePage()
            : this(ServiceProvider.GetRequiredService<VM>())
        {
        }
    }
}
