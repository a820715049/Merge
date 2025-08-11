/*
 * @Author: qun.chao
 * @Date: 2022-08-23 18:18:44
 */
using System;
using UnityEngine;
using Coffee.UIParticleExtensions;

public class UIEffectParticleHolder : MonoBehaviour
{
    public Action onAttractedHandler;
    public ParticleSystem ps;
    public float lifetimeDelta = 0.5f;

    public void SetAttractorPos(Vector3 pos)
    {
        transform.position = pos;
    }

    public void Emit(Vector3 pos, int count)
    {
        ps.transform.position = pos;
        if (count == 1)
        {
            var shape = ps.shape;
            var angle = shape.angle;
            shape.angle = 0f;
            ps.Emit(count);
            shape.angle = angle;
        }
        else
        {
            var main = ps.main;
            var origin = main.startLifetime;
            var lt = origin;
            for (int i = 0; i < count; i++)
            {
                lt.constant += i * lifetimeDelta;
                main.startLifetime = lt;
                ps.Emit(1);
            }
            main.startLifetime = origin;
        }
    }

    public void OnAttracted()
    {
        onAttractedHandler?.Invoke();
    }
}