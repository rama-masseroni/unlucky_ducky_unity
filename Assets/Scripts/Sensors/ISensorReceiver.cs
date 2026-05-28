public interface ISensorReceiver
{
    string SensorConnectionId { get; }

    void OnSensorActivated(SensorController sensor, GridWalkerController activator);
}
