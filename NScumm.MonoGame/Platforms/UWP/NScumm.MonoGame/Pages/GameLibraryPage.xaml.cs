using System.Diagnostics;
using NScumm.MonoGame.Converters;
using NScumm.MonoGame.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Microsoft.AdMediator.Core.Events;

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

            AdMediator_0A5E56.AdSdkError += OnAdSdkError;
            AdMediator_0A5E56.AdMediatorFilled += OnAdFilled;
            AdMediator_0A5E56.AdMediatorError += OnAdMediatorError;
            AdMediator_0A5E56.AdSdkEvent += OnAdSdkEvent;

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

        private void OnAdSdkEvent(object sender, AdSdkEventArgs e)
        {
            Debug.WriteLine("AdSdk event {0} by {1}", e.EventName, e.Name);
        }

        private void OnAdMediatorError(object sender, AdMediatorFailedEventArgs e)
        {
            Debug.WriteLine("AdMediatorError:" + e.Error + " " + e.ErrorCode);
            // if (e.ErrorCode == AdMediatorErrorCode.NoAdAvailable)
            // AdMediator will not show an ad for this mediation cycle
        }

        private void OnAdFilled(object sender, AdSdkEventArgs e)
        {
            Debug.WriteLine("AdFilled:" + e.Name);
        }

        private void OnAdSdkError(object sender, AdFailedEventArgs e)
        {
            Debug.WriteLine("AdSdkError by {0} ErrorCode: {1} ErrorDescription: {2} Error: {3}", e.Name, e.ErrorCode, e.ErrorDescription, e.Error);
        }
    }    
}
