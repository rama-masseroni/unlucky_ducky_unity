using UnityEngine;

public interface ISensorReceiver
{
    string SensorConnectionId { get; }

    void OnSensorActivated(SensorController sensor, Component activator);
}
