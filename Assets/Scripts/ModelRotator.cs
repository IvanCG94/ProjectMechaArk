using UnityEngine;
using UnityEngine.InputSystem;

public class ModelRotator : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 1.0f;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            float mouseY = Input.GetAxis("Mouse X");
            float rotationAmount = mouseY * _sensitivity;

            transform.Rotate(0, rotationAmount, 0);
        }
    }
}
