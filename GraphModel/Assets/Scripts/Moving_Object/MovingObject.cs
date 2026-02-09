using UnityEngine;
using UnityEngine.Serialization;

public class MovingObject : MonoBehaviour
{
    [SerializeField]
    private float _movementSpeed = 0.01f;
    [SerializeField]
    private float _rotationSpeed = 30f;
    [SerializeField]
    private Vector2 _curMovementVector = Vector2.zero;
    [SerializeField]
    private Vector3 _curMovementVector3D = Vector3.zero;
    [SerializeField]
    private float _curRotationScalar = 0f;

    // Update is called once per frame
    public void Update()
    {
        HandleMovement3D();


        _curRotationScalar = 0f;

        if (Input.GetKey(KeyCode.Q))
        {
            _curRotationScalar -= 1;
        }
        if (Input.GetKey(KeyCode.E))
        {
            _curRotationScalar += 1;
        }

        gameObject.transform.Rotate(0, _curRotationScalar * _rotationSpeed * Time.deltaTime, 0);
    }

    private void HandleMovement2D()
    {
        _curMovementVector = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
        {
            _curMovementVector += Vector2.up;
        }
        if (Input.GetKey(KeyCode.A))
        {
            _curMovementVector += Vector2.left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            _curMovementVector += Vector2.down;
        }
        if (Input.GetKey(KeyCode.D))
        {
            _curMovementVector += Vector2.right;
        }
        _curMovementVector *= _movementSpeed;
        _curMovementVector = gameObject.transform.rotation * _curMovementVector;

        gameObject.transform.localPosition += new Vector3(_curMovementVector.x, 0, _curMovementVector.y);
    }

    private void HandleMovement3D()
    {
        _curMovementVector3D = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            _curMovementVector3D += gameObject.transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            _curMovementVector3D -= gameObject.transform.right;
        }
        if (Input.GetKey(KeyCode.S))
        {
            _curMovementVector3D -= gameObject.transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            _curMovementVector3D += gameObject.transform.right;
        }
        _curMovementVector3D *= _movementSpeed;

        gameObject.transform.localPosition += new Vector3(_curMovementVector3D.x, 0, _curMovementVector3D.z);
    }
}
