/*
 * @Author: qun.chao
 * @Date: 2024-01-15 18:44:52
 */

/*
## 包内资源
- 各种.ab文件
- file_list.x (即AB依赖)
- 打包过程中，以上文件被上传到cdn，文件清单提交到conf的对应分支(如: res_ios.json)

## 包外资源
- 类似包内资源的处理流程
- 仅生成相关的.ab，和必要的external_file_list.x(即包外资源的AB依赖)
- 将以上文件上传到cdn，文件清单提交到conf的对应分支(如: res_ios_external.json)

## 补充
- 外部文件不应自动下载，而外部文件的'清单'应该自动下载
- 比如res_ios.json里记录了res_ios_external.json文件本身
```
{
	"file_list.x": "resource/file_list.x#4174#012709800011074ee88429629720600f",
	"map_common.ab": "resource/map_common.ab#96480#248cc7df97646d7cbf8dc263955ebf88",
    ...
	"res_ios_external.json": "resource/res_android_external.x#196#c081ca4737f3886279a1335b4ba6e85f",
}
```
*/

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "ExternalResConfig", menuName = "ScriptableObjects/ExternalResConfig", order = 1)]
public class ExternalResConfig : ScriptableObject
{
    public static readonly string config_path = $"Assets/CenturyGamePackageRes/AppBuilder/Editor/ExternalResConfig.asset";
    public string[] bundle_pattern;
    public string[] bundle_name;

    public static ExternalResConfig GetConfigInst()
    {
        var config = AssetDatabase.LoadAssetAtPath<ExternalResConfig>(config_path);
        if (!config)
        {
            throw new System.IO.FileNotFoundException($"file {config_path} not found!");
        }
        return config;
    }
}

#endif