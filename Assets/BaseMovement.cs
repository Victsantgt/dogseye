using UnityEngine;

public class BaseMovement : MonoBehaviour
{

    public float PlayerSpeed = 2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * PlayerSpeed);
    }
}
