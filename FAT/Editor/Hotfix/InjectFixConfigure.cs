/**
 * @Author: handong.liu
 * @Date: 2021-06-10 20:41:29
 */
using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using EL;
using IFix.Editor;
using IFix;
using System.Linq;

[Configure]
public static class InjectFixConfigure
{  
    private static readonly HashSet<string> kWhiteListNamespace = new HashSet<string>() {
        "Merge",
        "GameNet",
        "EL",
        "EL.Resource",
        "FAT",
        "FAT.Merge",
        "FAT.Platform"
    };
    [IFix]
    static IEnumerable<Type> hotfix
    {
        get
        {
            var ret = (from type in Assembly.Load("asmdef_fat_runtime").GetTypes() where (string.IsNullOrEmpty(type.Namespace) || kWhiteListNamespace.Contains(type.Namespace)) && _FiltType(type) select type);
            DebugEx.FormatInfo("InjectFixConfigure::hotfix -> {0}", ret);
            return ret;//new List<System.Type>(){};
        }
    }

    private static bool _FiltType(System.Type tp)
    {
        return tp.Name.IndexOf("Test") < 0 && !_IsProtobufCode(tp);
    }

    private static bool _IsProtobufCode(System.Type tp)
    {
        return tp.IsSubclassOf(typeof(Google.Protobuf.IMessage)) || (tp.Namespace == "GameNet" && tp.Name.IndexOf("Reflection") > 0);
    }
}