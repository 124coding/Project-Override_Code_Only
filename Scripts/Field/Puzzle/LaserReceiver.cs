using UnityEngine;

public class LaserReceiver : MonoBehaviour
{
    public PowerSource powerSource;

    public void ReceiveLaser()
    {
        powerSource.ReceiveLaser();
    }
}