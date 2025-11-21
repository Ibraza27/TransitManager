using CommunityToolkit.Mvvm.ComponentModel;

namespace TransitManager.WPF.ViewModels
{
    public class SplashViewModel : ObservableObject
    {
        private string _loadingStatus = "Initialisation...";

        public string LoadingStatus
        {
            get => _loadingStatus;
            set => SetProperty(ref _loadingStatus, value);
        }
        
        public string Version { get; }

        public SplashViewModel()
        {
             var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
             Version = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
        }
    }
}