namespace K8090.ManagedClient
{
    public class ButtonStatus
    {
        public int ButtonIndex { get; init; }
        public bool PressedNow { get; init; }
        public bool WasPressed { get; init; }
        public bool WasReleased { get; init; }

        public ButtonStatus(int index, bool pressedNow, bool wasPressed, bool wasReleased)
        {
            ButtonIndex = index;
            PressedNow = pressedNow;
            WasPressed = wasPressed;
            WasReleased = wasReleased;
        }
    }
}
