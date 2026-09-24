namespace LittleRoad
{
    // Human trials wait for a fresh, focused confirmation before time advances.
    // Automated races keep their existing countdown and simulation timeline.
    public sealed class ManualRaceStart
    {
        public bool Waiting {get;private set;}
        public ManualRaceStart(bool manual){Waiting=manual;}
        public bool TryStart(bool focused,bool requested)
        {
            if(!Waiting||!focused||!requested)return false;
            Waiting=false;return true;
        }
    }
}
