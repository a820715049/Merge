/**
 * @Author: handong.liu
 * @Date: 2021-02-20 16:14:42
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public static class Env
    {
        private static IMergeEnvironment sEnv;
        public static IMergeEnvironment Instance => sEnv;

        public static void SetEnv(IMergeEnvironment env)
        {
            sEnv = env;
        }
    }
}