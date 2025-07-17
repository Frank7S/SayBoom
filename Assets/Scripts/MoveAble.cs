using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
}

public class MoveAble : MonoBehaviour
{
    [SerializeField]
    private Collider2D collider;

    [Header("方向选择")]
    public Direction v_direction;

    [Header("移动位置")]
    [Range(1f, 6f)]
    public float distance;
    private bool isTigger = false;

    private Vector2[] directions = new Vector2[4]
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    void Start()
    {
        collider = GetComponent<Collider2D>();
        if (collider is null)
        {
            Debug.Log("do not have collider");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (collision.tag == "Player" && !isTigger)
        {
            //TODO 获取玩家大小类型  
            int type = collision.transform.GetComponent<AudioControlledPlayer>().Size;
        Debug.Log("Player Entered: " + collision.name ) ;

            if (type == 2)
            {
                Vector2 new_dir = directions[(int)v_direction] * distance;
                Vector3 new_V3_offset = new Vector3(new_dir.x, new_dir.y, 0f);
                transform.position += new_V3_offset;

                isTigger = true;
            }
        }
    }

}
