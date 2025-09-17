using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TransitManager.Core.Messages
{
    public class ConteneurUpdatedMessage : ValueChangedMessage<bool>
    {
        public ConteneurUpdatedMessage(bool value) : base(value) { }
    }
}