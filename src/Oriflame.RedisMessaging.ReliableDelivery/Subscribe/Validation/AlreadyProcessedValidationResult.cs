namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    public sealed class AlreadyProcessedValidationResult : IMessageValidationResult
    {
        public static readonly AlreadyProcessedValidationResult Instance  = new AlreadyProcessedValidationResult();

        private AlreadyProcessedValidationResult()
        {
        }
    }
}