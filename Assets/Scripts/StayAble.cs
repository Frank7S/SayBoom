using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StayAble : MonoBehaviour
{
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.tag == "Player")
        {

            collision.transform.GetComponent<AudioController>().Scale_flag = true;
        }
    }

    
    private void OnCollisionEnter2D(Collision2D other) {
        if (other.transform.tag == "Player")
        {

            other.transform.GetComponent<AudioController>().Scale_flag = false;
        }
    }
}
