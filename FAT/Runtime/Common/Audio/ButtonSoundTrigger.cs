/**
 * @Author: handong.liu
 * @Date: 2020-12-08 16:06:51
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using EL;

public class ButtonSoundTrigger : MonoBehaviour
{
    public string clickSound;
    private Button mCachedButton;
    public void Awake()
    {
        mCachedButton = GetComponent<Button>();
        mCachedButton.onClick.AddListener(_OnClick);
    }

    private void _OnClick()
    {
        FAT.Game.Manager.audioMan.TriggerSound(clickSound);
    }
    public static bool CheckAddSound(GameObject prefab)
    {
        return _CheckAddSoundRecursively(prefab.transform);
    }

    private static bool _CheckAddSoundRecursively(Transform root)
    {
        var changed = false;
        var btn = root.GetComponent<Button>();
        if(btn != null)
        {
            //a button
            var eventTrigger = root.GetComponent<ButtonSoundTrigger>();
            if(eventTrigger == null)
            {
                eventTrigger = root.gameObject.AddComponent<ButtonSoundTrigger>();
                eventTrigger.clickSound = "UIClick";
                // DebugEx.FormatTrace("ButtonSoundTrigger._CheckAddSoundRecursively ----> {0}", eventTrigger.transform.GetPath());
                changed = true;
            }
        }

        var childCount = root.childCount;
        for(int i = 0; i < childCount; i++)
        {
            changed = _CheckAddSoundRecursively(root.GetChild(i)) || changed;
        }
        return changed;
    }
}