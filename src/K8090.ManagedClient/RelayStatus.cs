namespace K8090.ManagedClient
{
    public class RelayStatus
    {
        public int RelayIndex { get; private set; }
        public bool PreviouslyOn { get; private set; }
        public bool CurrentlyOn { get; private set; }
        public bool TimerActive { get; private set; }

        public RelayStatus(int index, bool previouslyOn, bool currentlyOn, bool timerActive)
        {
            RelayIndex = index;
            PreviouslyOn = previouslyOn;
            CurrentlyOn = currentlyOn;
            TimerActive = timerActive;
        }
    }
}
