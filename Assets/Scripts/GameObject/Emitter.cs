using System.Collections;
using UnityEngine;

/// <summary>
/// 简易发射器：直接实例化public变量指定的Prefab，不用对象池
/// </summary>
public class Emitter : MonoBehaviour
{
    [Header("发射设置")]
    [Tooltip("直接拖拽发射物Prefab")] 
    public GameObject projectilePrefab;

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
    [SerializeField] private bool showDebugInfo = true;

    private Coroutine fireCoroutine;

    void Start()
    {
        if (autoStart)
            StartFiring();
    }

    public void StartFiring()
    {
        if (fireCoroutine == null)
        {
            fireCoroutine = StartCoroutine(FireLoop());
            if (showDebugInfo)
                Debug.Log($"[Emitter] 开始发射，间隔: {fireInterval}秒");
        }
    }

    public void StopFiring()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
            if (showDebugInfo)
                Debug.Log("[Emitter] 停止发射");
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
        if (projectilePrefab == null)
        {
            Debug.LogError("[Emitter] 请在Inspector拖拽发射物Prefab");
            return;
        }
        GameObject go = Instantiate(projectilePrefab, transform.position, transform.rotation);
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = fireDirection.normalized * fireSpeed;
            if (showDebugInfo)
                Debug.Log($"[Emitter] 发射物体，速度: {rb.velocity}");
        }
        else
        {
            Debug.LogWarning($"[Emitter] 发射物 {go.name} 没有Rigidbody2D组件，无法设置速度");
        }
        // 自动销毁
        Destroy(go, projectileLifeTime);
    }

    // Inspector调试用
    [ContextMenu("Test Fire Once")]
    public void TestFireOnce()
    {
        FireOnce();
    }

    void OnDrawGizmosSelected()
    {
        // 绘制发射方向
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, fireDirection.normalized * 2f);
        // 绘制发射器位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
} 