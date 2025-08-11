/**
 * @Author: handong.liu
 * @Date: 2021-03-30 14:50:56
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;


namespace EL.Resource 
{
    public interface IFileLoader
    {
        IEnumerator CoCheckImportantFiles(ResourceManifest container);   //注意：container里已经有数据了，包含了优先级更高的资源清单，因此如果file在container里，且md5不同，则需将其忽略不加载
        void LoadFile(string path, System.Action<byte[], string> cb/*filedata, error*/);
    }
}