using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public sealed class ArrowBullet : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float hitSweepRadius = 0.08f;

    private Rigidbody projectileRigidbody;
    private ArrowsPool pool;
    private Transform owner;
    private Vector3 startPosition;
    private Vector3 previousPosition;
    private float maxDistance;
    private float damage;
    private bool launched;
    private readonly RaycastHit[] sweepHits = new RaycastHit[16];

    private void Awake()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (projectileRigidbody == null)
            projectileRigidbody = GetComponent<Rigidbody>();

        launched = false;
        projectileRigidbody.velocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;
    }

    public void SetPool(ArrowsPool arrowsPool)
    {
        pool = arrowsPool;
    }

    public void Launch(
        Vector3 spawnPosition,
        Vector3 direction,
        float speed,
        float range,
        float attackDamage,
        Transform projectileOwner)
    {
        var normalizedDirection = direction.normalized;
        var spawnRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);
        projectileRigidbody.position = spawnPosition;
        projectileRigidbody.rotation = spawnRotation;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        owner = projectileOwner;
        startPosition = spawnPosition;
        previousPosition = spawnPosition;
        maxDistance = range;
        damage = attackDamage;
        launched = true;

        projectileRigidbody.velocity = normalizedDirection * speed;
        Debug.Log($"[ArrowDebug] Arrow launched. id={GetInstanceID()}, position={transform.position}, velocity={projectileRigidbody.velocity}, owner={owner.name}", this);
    }

    private void FixedUpdate()
    {
        if (!launched) return;

        SweepForDamage(previousPosition, transform.position);
        if (!launched) return;

        previousPosition = transform.position;
        if ((transform.position - startPosition).sqrMagnitude >= maxDistance * maxDistance)
            Release("max distance");
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other, "trigger hit");
    }

    private void SweepForDamage(Vector3 from, Vector3 to)
    {
        var offset = to - from;
        var distance = offset.magnitude;
        if (distance <= 0.0001f) return;

        var hitCount = Physics.SphereCastNonAlloc(
            from,
            hitSweepRadius,
            offset / distance,
            sweepHits,
            distance,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        for (var i = 0; i < hitCount && launched; i++)
            TryDamage(sweepHits[i].collider, "sweep hit");
    }

    private bool TryDamage(Collider other, string hitSource)
    {
        if (!launched || other == null || owner == null || other.transform.IsChildOf(owner))
            return false;

        var behaviours = other.GetComponentsInParent<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (!(behaviour is IDamageable damageable)) continue;
            Debug.Log($"[ArrowDebug] Damage hit. id={GetInstanceID()}, source={hitSource}, collider={other.name}, position={transform.position}, damage={damage:F1}", this);
            damageable.TakeDamage(damage);
            Release("damage hit");
            return true;
        }

        return false;
    }

    private void Release(string reason)
    {
        if (!launched) return;
        Debug.Log($"[ArrowDebug] Arrow released. id={GetInstanceID()}, reason={reason}, position={transform.position}", this);
        launched = false;
        owner = null;
        projectileRigidbody.velocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;

        if (pool != null) pool.Return(this);
        else Destroy(gameObject);
    }
}
