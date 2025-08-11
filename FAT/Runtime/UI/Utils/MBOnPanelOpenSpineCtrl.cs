/*
 * @Author: qun.chao
 * @Date: 2022-10-24 16:14:29
 */
using UnityEngine;
using Spine.Unity;

public class MBOnPanelOpenSpineCtrl : MonoBehaviour
{
    [SerializeField] private string enterAnim;
    [SerializeField] private string idleAnim;
    [SerializeField] private SkeletonGraphic skeleton;

    private void Awake()
    {
        if (skeleton == null)
        {
            skeleton = GetComponent<SkeletonGraphic>();
        }
    }

    private void OnEnable()
    {
        skeleton?.AnimationState.SetAnimation(0, enterAnim, false);
        skeleton?.AnimationState.AddAnimation(0, idleAnim, true, 0f);
    }
}