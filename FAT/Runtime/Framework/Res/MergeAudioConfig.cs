/**
 * @Author: handong.liu
 * @Date: 2021-04-13 12:22:43
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

[CreateAssetMenu(fileName = "cfg_merge_audio", menuName = "ScriptableObjects/MergeAudioConfig", order = 1)]
public class MergeAudioConfig : ScriptableObject
{
    public string[] mergeSounds;
    public SoundEventConfig soundEventConfig;

    
    public string GetMergeSoundByLevel(int level)
    {
        return mergeSounds.GetElementEx(level - 2, ArrayExt.OverflowBehaviour.Clamp);
    }
}