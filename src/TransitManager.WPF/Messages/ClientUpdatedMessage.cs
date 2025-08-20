using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TransitManager.WPF.Messages
{
    public class ClientUpdatedMessage : ValueChangedMessage<bool>
    {
        public ClientUpdatedMessage(bool value) : base(value) { }
    }
}