using UnityEngine;

public class FurniturePhysics : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private ShakeGroundObject shakeGroundObject;
    [SerializeField] private bool canBePushed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        bool earthquakeActive = shakeGroundObject != null && shakeGroundObject.isShaking;

        if (!earthquakeActive && !canBePushed)
        {
            // Fiziksel olarak kilitle
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            // Deprem varsa serbest bırak
            rb.constraints = RigidbodyConstraints.None;
        }
    }
}