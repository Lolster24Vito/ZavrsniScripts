using UnityEngine;
using System.Collections;

public class RegularBullet : MonoBehaviour
{
    public float lifeSeconds = 5f;
    public float impactForce = 30f;
    private Rigidbody rb;
    private bool isReturning = false;
    public System.Action<GameObject> onBulletDie;

    void Awake() => rb = GetComponent<Rigidbody>();

    public void Initialize(Vector3 pos, Vector3 dir, float speed)
    {
        transform.position = pos;
        rb.position = pos;
        isReturning = false;

        rb.isKinematic = false;
        rb.velocity = dir.normalized * speed;

        StartCoroutine(LifeTimer());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isReturning) return;

        // Hit logic
        if (collision.gameObject.CompareTag("Police"))
        {
            return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("NPC"))
        {
            Vector3 impactDir = rb.velocity.normalized;
            RagdollSwapper.Instance.SwapToRagdoll(collision.gameObject, impactForce, collision.GetContact(0).point, impactDir);
        }

        ReturnToPool();
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeSeconds);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturning) return;
        isReturning = true;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        onBulletDie?.Invoke(gameObject);
    }
}