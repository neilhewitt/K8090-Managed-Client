namespace K8090.ManagedClient
{
    public class RelayStatus
    {
        public int RelayIndex { get; init; }
        public bool PreviouslyOn { get; init; }
        public bool CurrentlyOn { get; init; }
        public bool TimerActive { get; init; }

        public RelayStatus(int index, bool previouslyOn, bool currentlyOn, bool timerActive)
        {
            RelayIndex = index;
            PreviouslyOn = previouslyOn;
            CurrentlyOn = currentlyOn;
            TimerActive = timerActive;
        }
    }
}
