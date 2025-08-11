/*
 * @Author: tang.yan
 * @Description: 引入插件解决enum类型序列化后可能会出现序号错乱的情况
 * @Description: https://github.com/dotsquid/StableEnum
 * @Date: 2025-05-22 11:05:45
 */
using System;

//将需要使用此插件的enum类型在下方定义，同时也提供StableEnum和具体enum类型之间的自动转换规则
namespace FAT
{
    [Serializable]
    public class FlyTypeEnum : StableEnum<FlyType>
    {
        // 隐式从 FlyTypeEnum 到 FlyType
        public static implicit operator FlyType(FlyTypeEnum e) => e.value;
        // 隐式从 FlyType 到 FlyTypeEnum
        public static implicit operator FlyTypeEnum(FlyType v) => new FlyTypeEnum { value = v };
    }
    
}