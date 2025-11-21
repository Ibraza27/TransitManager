// src/TransitManager.Core/Messages/UserUpdatedMessage.cs

namespace TransitManager.Core.Messages
{
    /// <summary>
    /// Message envoyé lorsqu'un utilisateur est créé, modifié ou supprimé.
    /// Il ne contient pas de données, il sert juste de notification pour rafraîchir les listes.
    /// </summary>
    public record UserUpdatedMessage();
}