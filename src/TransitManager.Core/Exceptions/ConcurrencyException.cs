using System;

namespace TransitManager.Core.Exceptions
{
    /// <summary>
    /// Exception levée lorsqu'un conflit de concurrence est détecté lors de la sauvegarde des données.
    /// </summary>
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message) : base(message)
        {
        }

        public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}