namespace Oriflame.RedisMessaging.ReliableDelivery.Subscribe.Validation
{
    public sealed class SuccessValidationResult : IMessageValidationResult
    {
        public static readonly SuccessValidationResult Instance  = new SuccessValidationResult();

        private SuccessValidationResult()
        {
        }
    }
}