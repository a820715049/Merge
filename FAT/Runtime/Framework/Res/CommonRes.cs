/**
 * @Author: handong.liu
 * @Date: 2020-08-28 14:52:11
 */

using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CommonRes", order = 1)]
public class CommonRes : ScriptableObject
{
    public static CommonRes Instance;
    public MergeAudioConfig soundConfig;
    public Material frozenItemMat;
    public Material grayImageMat;
    public Material grayWithAlphaImageMat;
    public Material monoImageMatCommon;
    public Material monoImageMat;
    public Sprite frozenCoverSprite;
    public Material gaussianBlurMat;    //高斯模糊材质球
    public Material buildingGlowMat;    //装饰区建筑升级发光材质球
}