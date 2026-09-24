namespace LittleRoad
{
    // How much of the car's physical grip and braking the AI plans to use.
    // These fractions were the AI's only "skill" setting and also its safety
    // reserve (ai-racing-review goal 3). Experiment --pace-margins raises them
    // one step; traffic safety (leader cap, immediate guard) is unchanged.
    public static class PaceMargins
    {
        static readonly bool raised=RaceTrial.Has("--pace-margins");
        // Cornering speed on the racing line and local path envelopes.
        public static readonly float Corner=raised?.84f:.78f;
        // Steering-curvature speed cap in the path followers.
        public static readonly float Steering=raised?.90f:.85f;
        // Braking in the precomputed racing-line speed profile.
        public static readonly float ProfileBrake=raised?.90f:.85f;
        // Multiplier on the driver's own braking fraction in local path envelopes (capped at 0.95).
        public static readonly float EnvelopeBrakeScale=raised?1.1f:1f;
    }
}
