/*
 * @Author: qun.chao
 * @Date: 2023-10-12 11:52:01
 */
using System;
using System.Collections.Generic;

namespace FAT
{
    public interface IGameModule
    {
        void Reset();       // 初始化 & 清理
        void LoadConfig();
        void Startup();
    }

    //Tick级时间驱动 有需要的Man可以使用此接口
    public interface IUpdate
    {
        void Update(float dt);  //默认参数为Time.deltaTime
    }

    //秒级时间驱动 有需要的Man可以使用此接口
    public interface ISecondUpdate
    {
        void SecondUpdate(float dt);    //默认参数均传1
    }
}