using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public sealed class ArrowBullet : MonoBehaviour
{
    private Rigidbody projectileRigidbody;
    private ArrowsPool pool;
    private Transform owner;
    private Vector3 startPosition;
    private float maxDistance;
    private float damage;
    private bool launched;

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
    }

    public void SetPool(ArrowsPool arrowsPool)
    {
        pool = arrowsPool;
    }

    public void Launch(
        Vector3 direction,
        float speed,
        float range,
        float attackDamage,
        Transform projectileOwner)
    {
        owner = projectileOwner;
        startPosition = transform.position;
        maxDistance = range;
        damage = attackDamage;
        launched = true;

        var normalizedDirection = direction.normalized;
        transform.forward = normalizedDirection;
        projectileRigidbody.velocity = normalizedDirection * speed;
    }

    private void FixedUpdate()
    {
        if (!launched) return;
        if ((transform.position - startPosition).sqrMagnitude >= maxDistance * maxDistance)
            Release();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || other.transform.root == owner) return;

        var behaviours = other.GetComponentsInParent<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (!(behaviour is IDamageable damageable)) continue;
            damageable.TakeDamage(damage);
            break;
        }

        Release();
    }

    private void Release()
    {
        if (!launched) return;
        launched = false;
        projectileRigidbody.velocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;

        if (pool != null) pool.Return(this);
        else Destroy(gameObject);
    }
}
