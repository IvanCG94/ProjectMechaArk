using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class RobotController : MonoBehaviour
{
    [Header("Estadísticas")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 15f;
    public float acceleration = 60f; 
    
    [Header("Físicas")]
    public float gravityForce = 20f;
    public LayerMask groundLayers; 

    // Referencias
    private Rigidbody _rb;
    private CapsuleCollider _col; // Nueva referencia
    private Transform _camTransform;
    private Vector3 _inputVector;
    private Vector3 _targetVelocity;
    private bool _isGrounded;
    private Vector3 _groundNormal;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>(); // Obtenemos el Collider

        // CONFIGURACIÓN OBLIGATORIA DEL RIGIDBODY
        _rb.useGravity = false; 
        _rb.freezeRotation = true; 
        _rb.interpolation = RigidbodyInterpolation.Interpolate; 
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (Camera.main != null) _camTransform = Camera.main.transform;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _inputVector = new Vector3(h, 0, v).normalized;
    }

    void FixedUpdate()
    {
        CheckGround();
        
        // --- DEBUG VISUAL: RAYCAST DINÁMICO ---
        // El Rayo empieza en el centro y se extiende hasta tocar el suelo + un margen
        float rayLength = (_col.height / 2f) + 0.1f;
        Vector3 rayOrigin = transform.position + transform.TransformDirection(_col.center);
        
        Debug.DrawRay(rayOrigin, 
                      -transform.up * rayLength, 
                      _isGrounded ? Color.green : Color.red);
        // ---------------------------------------

        CalculateTargetVelocity();
        ApplyMovement();
        ApplyRotation();
        ApplyGravity();
    }

    void CheckGround()
    {
        // 1. Definir Origen y Distancia de chequeo
        Vector3 origin = transform.position + transform.TransformDirection(_col.center);
        float rayStartHeight = _col.height / 2f; 
        float margin = 0.1f; // 10cm de margen extra
        
        // 2. Ejecutar Raycast
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayStartHeight + margin, groundLayers))
        {
            _isGrounded = true;
            _groundNormal = hit.normal;
            
            // Lógica de pendiente: Asume que no hay paredes verticales todavía
            if (Vector3.Angle(Vector3.up, _groundNormal) > 45f) 
            {
                _isGrounded = false; 
                _groundNormal = Vector3.up;
            }
        }
        else
        {
            _isGrounded = false;
            _groundNormal = Vector3.up;
        }
    }

    void CalculateTargetVelocity()
    {
        Vector3 camFwd = _camTransform.forward;
        Vector3 camRight = _camTransform.right;
        camFwd.y = 0; camRight.y = 0;
        camFwd.Normalize(); camRight.Normalize();

        Vector3 moveDir = (camFwd * _inputVector.z + camRight * _inputVector.x);

        Vector3 slopeMoveDir = Vector3.ProjectOnPlane(moveDir, _groundNormal).normalized;

        _targetVelocity = slopeMoveDir * moveSpeed;
    }

    void ApplyMovement()
    {
        if (_isGrounded)
        {
            Vector3 currentVel = _rb.linearVelocity;
            Vector3 newVel = Vector3.MoveTowards(currentVel, _targetVelocity, acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = newVel;
        }
        else
        {
            Vector3 airVelocity = new Vector3(_targetVelocity.x, _rb.linearVelocity.y, _targetVelocity.z);
            _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, airVelocity, (acceleration * 0.5f) * Time.fixedDeltaTime);
        }
    }

    void ApplyRotation()
    {
        if (_inputVector.sqrMagnitude > 0.01f)
        {
            Vector3 lookDir = _camTransform.forward * _inputVector.z + _camTransform.right * _inputVector.x;
            lookDir.y = 0;
            
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    void ApplyGravity()
    {
        if (!_isGrounded)
        {
            _rb.AddForce(Vector3.down * gravityForce * _rb.mass);
        }
        else
        {
            _rb.AddForce(-_groundNormal * 10f * _rb.mass);
        }
    }
}