namespace K8090.ManagedClient
{
    public class ButtonStatus
    {
        public int RelayIndex { get; init; }
        public bool PressedNow { get; init; }
        public bool WasPressed { get; init; }
        public bool WasReleased { get; init; }

        public ButtonStatus(int index, bool pressedNow, bool wasPressed, bool wasReleased)
        {
            RelayIndex = index;
            PressedNow = pressedNow;
            WasPressed = wasPressed;
            WasReleased = wasReleased;
        }
    }
}
