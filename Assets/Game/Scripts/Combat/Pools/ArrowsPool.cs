using System.Collections.Generic;
using UnityEngine;

public sealed class ArrowsPool : MonoBehaviour
{
    [SerializeField] private ArrowBullet arrowPrefab;
    [SerializeField, Min(1)] private int initialSize = 12;

    private readonly Queue<ArrowBullet> availableArrows = new Queue<ArrowBullet>();

    private void Awake()
    {
        for (var i = 0; i < initialSize; i++)
            availableArrows.Enqueue(CreateArrow());
    }

    public ArrowBullet Get()
    {
        var arrow = availableArrows.Count > 0
            ? availableArrows.Dequeue()
            : CreateArrow();

        // A dynamic Rigidbody must not remain under the pool transform while flying.
        arrow.transform.SetParent(null, true);
        arrow.gameObject.SetActive(true);
        Debug.Log($"[ArrowDebug] Pool.Get arrow={arrow.GetInstanceID()}, poolPosition={transform.position}, arrowPosition={arrow.transform.position}", arrow);
        return arrow;
    }

    public void Return(ArrowBullet arrow)
    {
        if (arrow == null || !arrow.gameObject.activeSelf) return;
        arrow.gameObject.SetActive(false);
        arrow.transform.SetParent(transform, false);
        availableArrows.Enqueue(arrow);
    }

    private ArrowBullet CreateArrow()
    {
        var arrow = Instantiate(arrowPrefab, transform);
        arrow.SetPool(this);
        arrow.gameObject.SetActive(false);
        return arrow;
    }
}
