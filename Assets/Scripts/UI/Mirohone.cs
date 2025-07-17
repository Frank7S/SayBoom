using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mirohone : MonoBehaviour
{
    public Button button;
    public Transform player;


    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        player.GetComponent<AudioController>().UI_control_flag = !player.GetComponent<AudioController>().UI_control_flag;

    }
}
