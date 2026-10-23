using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Viên đạn do Skill_Caster bắn ra. Bay thẳng theo hướng nhắm, chạm mục tiêu thì gây sát thương (Damage_Info dựng sẵn lúc bắn),
/// chạm vật cản thì biến mất, hết thời gian thì biến mất.
/// - Tự đảm bảo có Rigidbody (kinematic) + Collider (trigger), nên prefab chỉ cần phần hình ảnh; muốn đạn to hơn hãy
///   tự thêm Collider vào prefab.
/// - Mỗi mục tiêu chỉ bị trúng một lần bởi cùng một viên đạn (kể cả khi xuyên).
/// - Không cần gắn tay: bỏ trống prefab trong Skill_Delivery thì Skill_Projectile.Create_Default tự tạo quả cầu thử nghiệm.
/// </summary>
public class Skill_Projectile : MonoBehaviour
{
    private const float DefaultRadius = 0.25f;

    private Character_Stats owner;
    private Damage_Info info;
    private Vector3 direction;
    private float speed;
    private int remainingHits;
    private LayerMask targetMask;
    private LayerMask obstacleMask;
    private bool launched;
    private bool finished;

    private readonly HashSet<Character_Stats> alreadyHit = new HashSet<Character_Stats>();

    private void Awake()
    {
        EnsurePhysics();
    }

    private void EnsurePhysics()
    {
        if (!TryGetComponent(out Rigidbody body)) body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = DefaultRadius;
            colliders = new Collider[] { sphere };
        }

        foreach (Collider c in colliders) c.isTrigger = true;
    }

    /// <summary>Tạo đạn thử nghiệm (quả cầu nhỏ) khi Skill_Delivery chưa gán prefab.</summary>
    public static Skill_Projectile Create_Default(Vector3 position, Quaternion rotation)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Projectile_Default";
        sphere.transform.SetPositionAndRotation(position, rotation);
        sphere.transform.localScale = Vector3.one * DefaultRadius * 2f;

        // Giữ SphereCollider của quả cầu (bán kính thế giới = DefaultRadius); Awake của Skill_Projectile sẽ chuyển nó thành trigger.
        return sphere.AddComponent<Skill_Projectile>();
    }

    /// <summary>Bắn đạn đi. targetMask: Layer mục tiêu; obstacleMask: Layer vật cản (tường...). Đạn tự huỷ sau lifetime giây.</summary>
    public void Launch(Character_Stats owner, Damage_Info info, Vector3 direction, Skill_Delivery delivery,
                       LayerMask targetMask, LayerMask obstacleMask)
    {
        this.owner = owner;
        this.info = info;
        this.direction = direction.normalized;
        this.targetMask = targetMask;
        this.obstacleMask = obstacleMask;

        speed = delivery.projectileSpeed;
        remainingHits = Mathf.Max(0, delivery.pierce) + 1;
        launched = true;

        Destroy(gameObject, delivery.projectileLifetime);
    }

    private void Update()
    {
        if (!launched || finished) return;

        transform.position += direction * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || finished || other.isTrigger) return;

        int layerBit = 1 << other.gameObject.layer;

        if ((targetMask.value & layerBit) != 0)
        {
            Character_Stats target = other.GetComponentInParent<Character_Stats>();

            if (target == null)
            {
                // Layer mục tiêu nhưng không có chỉ số (vật thể không phá được): coi như vật cản.
                Finish();
                return;
            }

            if (target == owner || target.IsDead || !alreadyHit.Add(target)) return;

            Damage_Result result = target.TakeDamage(info);
            Combat_Events.Report(owner, target, result);

            remainingHits--;
            if (remainingHits <= 0) Finish();
        }
        else if ((obstacleMask.value & layerBit) != 0)
        {
            Finish();
        }
    }

    private void Finish()
    {
        finished = true;
        Destroy(gameObject);
    }
}
