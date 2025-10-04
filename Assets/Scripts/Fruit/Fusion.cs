using UnityEngine;

public class Fusion : MonoBehaviour
{
    private void Awake()
    {
        // 모든 자식 오브젝트의 Collider2D 가져오기
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
        {
            // 충돌 이벤트 중계용 스크립트 붙이기
            ChildCollisionListener listener = col.gameObject.AddComponent<ChildCollisionListener>();
            listener.Init(this);
        }
    }

    // 자식에서 충돌 보고할 때 호출됨
    public void OnChildCollisionEnter(Collision2D collision, Collider2D self)
    {
        Fusion otherFusion = collision.collider.GetComponentInParent<Fusion>();
        if (otherFusion != null && otherFusion != this)
        {
            Destroy(gameObject);
            Destroy(otherFusion.gameObject);
        }
    }
}

// 자식에 붙을 리스너
public class ChildCollisionListener : MonoBehaviour
{
    private Fusion parentFusion;

    public void Init(Fusion parent)
    {
        parentFusion = parent;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (parentFusion != null)
        {
            parentFusion.OnChildCollisionEnter(collision, GetComponent<Collider2D>());
        }
    }
}
