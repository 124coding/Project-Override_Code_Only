using UnityEngine;

public interface IWorkObject
{
    void WorkOn();  // 신호를 받았을 때
    void WorkOff(); // 신호가 끊겼을 때
}
