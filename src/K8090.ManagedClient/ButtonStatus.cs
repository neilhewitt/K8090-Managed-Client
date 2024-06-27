namespace K8090.ManagedClient
{
    public class ButtonStatus
    {
        public int ButtonIndex { get; private set; }
        public bool PressedNow { get; private set; }
        public bool WasPressed { get; private set; }
        public bool WasReleased { get; private set; }

        public ButtonStatus(int index, bool pressedNow, bool wasPressed, bool wasReleased)
        {
            ButtonIndex = index;
            PressedNow = pressedNow;
            WasPressed = wasPressed;
            WasReleased = wasReleased;
        }
    }
}
