/**
 * @Author: handong.liu
 * @Date: 2020-12-08 15:29:34
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EL;

[CreateAssetMenu(fileName = "cfg_sound", menuName = "ScriptableObjects/SoundEventConfig", order = 1)]
public class SoundEventConfig : ScriptableObject
{
    [Serializable]
    public class EventData
    {
        public string eventName;
        public string[] clipNames;
        public int priority;
        public bool noInterrupt;
        public bool loop;
        public int channel;
    }
    [Serializable]
    public class ChannelData
    {
        public int capacity;
    }

    public EventData[] eventDatas;
    public ChannelData[] channels;
}