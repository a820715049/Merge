/**
 * @Author: handong.liu
 * @Date: 2020-09-28 11:11:02
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace GameNet
{
    public static class HttpPath
    {
        public const string AUTH = "msg.AuthReq";
        public const string LOGIN = "msg.LoginReq";
        public const string CHANGE_ROLE = "msg.ChangeRoleReq";
        public const string CREATE_ROLE = "msg.CreateRoleReq";
    }
}