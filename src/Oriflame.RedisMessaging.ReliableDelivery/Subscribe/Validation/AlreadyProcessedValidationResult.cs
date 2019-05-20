namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    /// <summary>
    /// Represents a validation result for the case when currently being processed was already proceed,
    /// hence it is being processed again.
    /// See <see cref="IMessageValidator.Validate(Message)" />
    /// </summary>
    public sealed class AlreadyProcessedValidationResult : IMessageValidationResult
    {
        /// <summary>
        /// A singleton instance for this result type
        /// </summary>
        public static readonly AlreadyProcessedValidationResult Instance  = new AlreadyProcessedValidationResult();

        private AlreadyProcessedValidationResult()
        {
        }
    }
}