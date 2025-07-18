using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using PoolNamespace; // 假设PoolMgr在全局命名空间

/// <summary>
/// 发射器：定时从对象池发射指定预制体
/// </summary>
public class Emitter : MonoBehaviour
{
    [Header("发射设置")]
    [Tooltip("发射物Prefab的Resources路径名（如Prefab/Item/breakbrick）")]
    public string projectilePrefabName = "Prefab/Item/breakbrick";

    [Tooltip("发射间隔（秒）")]
    public float fireInterval = 1.0f;

    [Tooltip("发射方向（世界坐标）")]
    public Vector2 fireDirection = Vector2.right;

    [Tooltip("发射速度")]
    public float fireSpeed = 5f;

    [Tooltip("发射物存活时间（秒）")]
    public float projectileLifeTime = 5f;

    [Header("调试")] 
    public bool autoStart = true;

    private Coroutine fireCoroutine;

    void Start()
    {
        if (autoStart)
            StartFiring();
    }

    public void StartFiring()
    {
        if (fireCoroutine == null)
            fireCoroutine = StartCoroutine(FireLoop());
    }

    public void StopFiring()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    private IEnumerator FireLoop()
    {
        while (true)
        {
            FireOnce();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    private void FireOnce()
    {
        PoolMgr.GetInstance().GetObj(projectilePrefabName, (go) =>
        {
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.SetActive(true);
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = fireDirection.normalized * fireSpeed;
            }
            // 自动回收
            StartCoroutine(ReleaseAfterTime(go, projectilePrefabName, projectileLifeTime));
        });
    }

    private IEnumerator ReleaseAfterTime(GameObject go, string name, float time)
    {
        yield return new WaitForSeconds(time);
        if (go != null && go.activeInHierarchy)
        {
            PoolMgr.GetInstance().PushObj(name, go);
        }
    }

    // Inspector调试用
    [ContextMenu("Test Fire Once")]
    public void TestFireOnce()
    {
        FireOnce();
    }
} 