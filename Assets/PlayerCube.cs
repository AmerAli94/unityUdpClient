using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour
{

    [SerializeField]
    float speed = 100.0f;

    // Update is called once per frame
    void Update()
    {
        // implementing movemnt using the default input axis in Unity Editor
        // Uses vector3 and effects topdown,leftright movement.
        Vector3 movementVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        movementVector *= speed * Time.deltaTime;
        transform.position += movementVector;
    }
}
