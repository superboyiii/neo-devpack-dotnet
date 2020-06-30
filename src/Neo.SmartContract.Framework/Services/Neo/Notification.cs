namespace Neo.SmartContract.Framework.Services.Neo
{
    public class Notification : IApiInterface
    {
        /// <summary>
        /// Sender script hash
        /// </summary>
        public readonly byte[] ScriptHash;

        /// <summary>
        /// Notification's name
        /// </summary>
        public readonly string EventName;

        /// <summary>
        /// Notification's state
        /// </summary>
        public readonly object[] State;
    }
}
