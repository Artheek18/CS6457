using UnityEngine;

public class WeaponContoller : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Prop"))
        {
            Debug.Log("Player has picked up the weapon!");
            Destroy(other.gameObject); // Remove the weapon from the scene
        }
    }
}
