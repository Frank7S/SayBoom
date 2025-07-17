using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mirohone : MonoBehaviour
{
    public Toggle toggle;
    public Transform player;


    private void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (toggle != null)
        {
               if (toggle.isOn)
            {
                player.GetComponent<AudioController>().UI_control_flag = true;
            }
            else
            {
                player.GetComponent<AudioController>().UI_control_flag = false;
            }
        }
        else
        {
            Debug.LogWarning("Toggle component not found!");
        }
        
        
        

    }
}
