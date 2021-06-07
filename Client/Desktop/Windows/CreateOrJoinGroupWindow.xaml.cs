using System.Windows;

namespace Slacek.Client.Desktop
{
    /// <summary>
    /// Interaction logic for CreateOrJoinGroupWindow.xaml
    /// </summary>
    public partial class CreateOrJoinGroupWindow : Window
    {
        public CreateOrJoinGroupWindow()
        {
            InitializeComponent();
            DataContext = ServiceProvider.GetRequiredService<CreateOrJoinGroupWindowViewModel>();
        }
    }
}
