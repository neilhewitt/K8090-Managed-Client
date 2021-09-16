namespace K8090.ManagedClient
{
    public enum Response
    {
        None = 0x00,
        QueryButtonMode = 0x22,
        QueryRelayTimerDelay = 0x44,
        ButtonStatus = 0x50,
        RelayStatus = 0x51,
        JumperStatus = 0x70,
        FirmwareVersion = 0x71
    }
}
