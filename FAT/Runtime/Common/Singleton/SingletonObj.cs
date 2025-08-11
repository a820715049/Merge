/**
 * @Author: handong.liu
 * @Date: 2020-12-15 11:40:33
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public class Singletonize<T> where T : new()
{
    public static T Instance = new T();
}