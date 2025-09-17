using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TransitManager.Core.Messages
{
    public class ClientUpdatedMessage : ValueChangedMessage<bool>
    {
        public ClientUpdatedMessage(bool value) : base(value) { }
    }
}