using NScumm.MonoGame.Converters;
using NScumm.MonoGame.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NScumm.MonoGame
{
    public sealed partial class GameLibraryPage
    {
        public IGameLibraryViewModel ViewModel
        {
            get { return DataContext as IGameLibraryViewModel; }
            set { DataContext = value; }
        }

        public GameLibraryPage()
        {
            InitializeComponent();
            DataContext = new GameLibraryViewModel();
            NoGameTextBlock.SetBinding(VisibilityProperty, new Binding { Path = new PropertyPath("ShowNoGameMessage"), Converter = new ShowNoGameMessageToVisibilityConverter() });
            ProgressPanel.SetBinding(VisibilityProperty, new Binding { Path = new PropertyPath("IsScanning"), Converter = new IsScanningToVisibilityConverter() });
        }

        private void OnLaunchGame(object sender, Windows.UI.Xaml.Controls.ItemClickEventArgs e)
        {
            // navigate to the game
            var vm = e.ClickedItem as GameViewModel;
            if (vm != null)
            {
                Frame.Navigate(typeof(GamePage), vm.Game);
            }
        }
    }    
}
