using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class AudioMgr : MonoBehaviour
{
    [SerializeField] private AudioSource[] audios;

    [Header("进入房间设置")] [Space(10)] 
    [Tooltip("最终需要的声音大小")]
    [SerializeField] private float enter_volume = 0.3f;
    [Tooltip("每次变化的声音大小")]
    [SerializeField] private float enter_span = -0.1f;
    [Tooltip("每次变化的间隔")]
    [SerializeField] private int enter_timer = 500;

    [Header("进入房间设置")] [Space(10)] 

    [SerializeField] private float exit_volume = 1f;
    [SerializeField] private float exit_span = 0.2f;
    [SerializeField] private int exit_timer = 400;

    private bool isPlay;
    private float temNum;


    private void Start()
    {
        temNum = audios[0].volume;
    }

    private void Update()
    {
        foreach (AudioSource source in audios)
        {
            source.volume = temNum;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{other.name} Enter");
        isPlay = false;
        SetVolume(enter_volume, enter_span, enter_timer);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"{other.name} Exit");
        isPlay = false;
        SetVolume(exit_volume, exit_span, exit_timer);
    }

    /// <summary>
    /// 异步设置声音线性变化
    /// </summary>
    /// <param name="value"></param>
    /// <param name="span"></param>
    /// <param name="timer"></param>
    async void SetVolume(float value, float span, int timer)
    {
        await Task.Run(() =>
        {
            while (!isPlay)
            {
                Thread.Sleep(timer);
                temNum += span;
                if (span > 0 && temNum >= value)
                {
                    isPlay = true;
                    temNum = value;
                }
                else if (span < 0 && temNum <= value)
                {
                    isPlay = true;
                    temNum = value;
                }
            }
        });
    }
}