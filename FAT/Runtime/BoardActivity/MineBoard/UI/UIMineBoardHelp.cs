/**
 * @Author: zhangpengjian
 * @Date: 2025/3/24 10:47:16
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/24 10:47:16
 * Description: 
 */

using EL;
using FAT;
using TMPro;
using UnityEngine;

public class UIMineBoardHelp : UIActivityHelp
{
    [SerializeField]
    private TMP_Text text1;
    [SerializeField]
    private TMP_Text text2;
    [SerializeField]
    private TMP_Text desc;
    private MineBoardActivity activity;

    protected override void OnCreate()
    {
        base.OnCreate();
    }
    
    protected override void OnParse(params object[] items)
    {
        activity = items[0] as MineBoardActivity;
    }

    protected override void OnPreOpen()
    {
        base.OnPreOpen();
        if (activity == null)
        {
            return;
        }
        var id = activity.ConfD.TokenId;
        var s = UIUtility.FormatTMPString(id);
        text1.SetText(I18N.FormatText("#SysComDesc904", s));
        text2.SetText(I18N.FormatText("#SysComDesc905", s));
        desc.SetText(I18N.FormatText("#SysComDesc957", s));
    }
}