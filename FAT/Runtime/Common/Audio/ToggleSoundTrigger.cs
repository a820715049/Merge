/**
 * @Author: handong.liu
 * @Date: 2020-12-08 18:26:04
 */
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Collections;
using System.Collections.Generic;
using EL;

public class ToggleSoundTrigger : MonoBehaviour, IPointerClickHandler
{
    public string clickSound;
    private Toggle mCachedToggle;
    private UISimpleToggle mCachedSimpleToggle;
    public void Awake()
    {
        mCachedToggle = GetComponent<Toggle>();
        mCachedSimpleToggle = GetComponent<UISimpleToggle>();
    }
    
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
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
        if(root.GetComponent<Toggle>() != null || root.GetComponent<UISimpleToggle>() != null)
        {
            //a button
            var eventTrigger = root.GetComponent<ToggleSoundTrigger>();
            if(eventTrigger == null)
            {
                eventTrigger = root.gameObject.AddComponent<ToggleSoundTrigger>();
                eventTrigger.clickSound = "Tab";
                DebugEx.FormatTrace("ToggleSoundTrigger._CheckAddSoundRecursively ----> {0}", eventTrigger.transform.GetPath());
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