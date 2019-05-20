namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    /// <summary>
    /// Represent a result object for a message validation without errors.
    /// See <see cref="IMessageValidator.Validate(Message)" />
    /// </summary>
    public sealed class SuccessValidationResult : IMessageValidationResult
    {
        /// <summary>
        /// A singleton instance for this result type
        /// </summary>
        public static readonly SuccessValidationResult Instance  = new SuccessValidationResult();

        private SuccessValidationResult()
        {
        }
    }
}