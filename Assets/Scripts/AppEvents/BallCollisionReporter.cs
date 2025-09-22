using UnityEngine;

public class BallCollisionReporter : MonoBehaviour
{
    void OnCollisionEnter(Collision c)
    {

        if (c.impulse.magnitude > 0.25f)
        {
            EventManager.TriggerEvent<BombBounceEvent, Vector3>(c.contacts[0].point);

        }
            

    }
}
