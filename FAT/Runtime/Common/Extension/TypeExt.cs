/*
 * @Author: qun.chao
 * @Date: 2023-10-17 20:05:18
 */
using System;

// https://stackoverflow.com/a/19317229
namespace EL
{
    public static class TypeExt
    {
        public static bool ImplementsInterface(this Type type, Type ifaceType)
        {
            Type[] intf = type.GetInterfaces();
            for (int i = 0; i < intf.Length; i++)
            {
                if (intf[i] == ifaceType)
                {
                    return true;
                }
            }
            return false;
        }
    }
}