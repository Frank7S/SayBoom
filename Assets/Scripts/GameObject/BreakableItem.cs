using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableItem : MonoBehaviour
{
    private int playerSize = 0; // 0=小，1=中，2=大

    void Awake()
    {
        // 订阅玩家体型变化事件
        EventCenter.GetInstance().AddEventListener<int>("PlayerSizeChanged", OnPlayerSizeChanged);
    }

    void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        EventCenter.GetInstance().RemoveEventListener<int>("PlayerSizeChanged", OnPlayerSizeChanged);
    }

    // 事件回调，记录当前玩家体型
    void OnPlayerSizeChanged(int size)
    {
        playerSize = size;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && playerSize ==2)
        {
            Debug.Log("玩家体型为：" + playerSize);
            Destroy(gameObject);
        }
    }
}
