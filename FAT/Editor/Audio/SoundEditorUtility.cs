/**
 * @Author: handong.liu
 * @Date: 2020-12-08 17:31:32
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using EL;


public static class SoundEditorUtility
{
    public static bool CheckAddSound(GameObject prefab)
    {
        var btnAdded = ButtonSoundTrigger.CheckAddSound(prefab);
        var toggleAdded = ToggleSoundTrigger.CheckAddSound(prefab);
        return btnAdded || toggleAdded;
    }
}