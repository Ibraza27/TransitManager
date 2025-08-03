using System.Threading.Tasks;

namespace TransitManager.WPF.ViewModels
{
    public class NotificationsViewModel : BaseViewModel
    {
        public NotificationsViewModel()
        {
            Title = "Notifications";
        }

        public override async Task InitializeAsync()
        {
            await LoadNotificationsAsync();
        }

        private Task LoadNotificationsAsync()
        {
            // TODO: Logique pour charger les notifications depuis le service
            return Task.CompletedTask;
        }
    }
}