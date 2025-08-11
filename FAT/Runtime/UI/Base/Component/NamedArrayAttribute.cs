// https://forum.unity.com/threads/how-to-change-the-name-of-list-elements-in-the-inspector.448910/
using System;
using UnityEngine;

public class NamedArrayAttribute : PropertyAttribute
{
    public readonly string[] names;
    public Type et;
    public NamedArrayAttribute(string[] names) { this.names = names; }
    // "an attribute argument must be a constant expression"
    public NamedArrayAttribute(Type t) { et = t; }
}