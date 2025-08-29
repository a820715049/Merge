/**
 * @Author: zhangpengjian
 * @Date: 2024/12/27 10:47:16
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/12/27 10:47:16
 * Description: 活动是否通关
 */

namespace FAT
{
    interface IActivityComplete
    {
        bool HasComplete();
        bool IsActive => true;
    }
}