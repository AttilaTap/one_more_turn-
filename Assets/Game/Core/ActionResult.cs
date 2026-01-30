namespace OneMoreTurn.Core
{
    /// <summary>
    /// Result of a player action (Bank, Push, Sacrifice).
    /// </summary>
    public class ActionResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; }
        public RunState NewState { get; set; }

        // Action-specific data
        public long AmountBanked { get; set; }      // For Bank action
        public float RiskAdded { get; set; }        // For Push action
        public string SacrificedModifierId { get; set; }  // For Sacrifice action

        public static ActionResult Fail(string reason)
        {
            return new ActionResult
            {
                Success = false,
                FailureReason = reason
            };
        }

        public static ActionResult Ok(RunState newState)
        {
            return new ActionResult
            {
                Success = true,
                NewState = newState
            };
        }
    }
}
