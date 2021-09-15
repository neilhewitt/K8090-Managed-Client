namespace K8090.ManagedClient
{
    public enum Command
    {
        All = 0x00,
        RelayOn = 0x11,
        RelayOff = 0x12,
        RelayToggle = 0x14,
        QueryRelayState = 0x18,
        SetButtonMode = 0x21,
        QueryButtonMode = 0x22,
        StartRelayTimer = 0x41,
        SetRelayTimerDelay = 0x42,
        QueryRelayTimerDelay = 0x44,
        ButtonStatus = 0x50,
        RelayStatus = 0x51,
        ResetFactoryDefaults = 0x66,
        JumperStatus = 0x70,
        FirmwareVersion = 0x71
    }
}
