namespace OneMoreTurn.Core
{
    /// <summary>
    /// Evaluates modifier conditions against the current run state.
    /// </summary>
    public static class ConditionEvaluator
    {
        public static bool Evaluate(ModifierCondition condition, RunState state)
        {
            if (condition == null) return true;

            return condition.Type switch
            {
                ConditionType.None => true,
                ConditionType.RiskAbove => state.Risk > condition.Threshold,
                ConditionType.RiskBelow => state.Risk < condition.Threshold,
                ConditionType.TurnAbove => state.Turn > condition.Threshold,
                ConditionType.TurnBelow => state.Turn < condition.Threshold,
                ConditionType.TurnMultiple => condition.TurnMultiple > 0 && state.Turn % condition.TurnMultiple == 0,
                ConditionType.FlagSet => !string.IsNullOrEmpty(condition.Flag) && state.HasFlag(condition.Flag),
                ConditionType.FlagNotSet => string.IsNullOrEmpty(condition.Flag) || !state.HasFlag(condition.Flag),
                ConditionType.CounterAbove => state.GetCounter(condition.Counter) > condition.Threshold,
                ConditionType.CounterBelow => state.GetCounter(condition.Counter) < condition.Threshold,
                ConditionType.HasModifier => !string.IsNullOrEmpty(condition.ModifierId) && state.HasModifier(condition.ModifierId),
                ConditionType.ScoreAbove => state.TotalScore > (long)condition.Threshold,
                ConditionType.ScoreBelow => state.TotalScore < (long)condition.Threshold,
                _ => true
            };
        }
    }
}
