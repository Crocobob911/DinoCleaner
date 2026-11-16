using System;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Cleaning
{
    // 오물 종류별로 각 도구가 얼마나 빨리 지우는지.
    // 1 = 물총 Strength 그대로, 0.1 = 10배 느리게, 0 = 안 지워짐
    [CreateAssetMenu(menuName = "DinoCleaner/Dirt Clean Rules", fileName = "DirtCleanRules")]
    public class DirtCleanRules : ScriptableObject
    {
        [Header("진흙 (R)")]
        public ToolRates mud = new ToolRates { water = 1f, pressure = 1f, soap = 0f };
        [Header("이끼·화산재 (G)")]
        public ToolRates moss = new ToolRates { water = 0.1f, pressure = 1f, soap = 0f };
        [Header("익룡 배설물 (B)")]
        public ToolRates poop = new ToolRates { water = 1f, pressure = 1f, soap = 0f }; // 거품 있는 경우에만

        [Header("비누 거품 (A)")]
        [Tooltip("비누가 거품을 바르는 속도 (Strength 배수)")]
        [Min(0f)] public float foamApply = 2f;
        [Tooltip("물·고압이 거품을 헹구는 속도. 배설물이 남은 곳은 안 헹궈짐")]
        [Min(0f)] public float foamRinse = 1f;
        [Tooltip("배설물이 지워지려면 필요한 거품 양 (0~1)")]
        [Range(0.05f, 1f)] public float foamRequired = 0.5f;

        // 이 노즐(플래그 조합)이 각 채널을 얼마나 빨리 지우는지(R, G, B, 0)
        public Vector4 GetRates(CleanerFlags flags)
        { 
            return new Vector4(mud.For(flags), moss.For(flags), poop.For(flags), 0f);
        }

        public Vector4 GetFoam(CleanerFlags flags)
        {
            float apply = (flags & CleanerFlags.Soap) != 0 ? foamApply : 0f;
            float rinse = (flags & (CleanerFlags.Water | CleanerFlags.Pressure)) != 0 ? foamRinse : 0f;
            return new Vector4(apply, rinse, foamRequired, 0f);
        }
    }

    [Serializable]
    public struct ToolRates
    {
        [Min(0f)] public float water;
        [Min(0f)] public float pressure;
        [Min(0f)] public float soap;

        public float For(CleanerFlags flags)
        {
            float rate = 0f;
            if ((flags & CleanerFlags.Water) != 0) rate = Mathf.Max(rate, water);
            if ((flags & CleanerFlags.Pressure) != 0) rate = Mathf.Max(rate, pressure);
            if ((flags & CleanerFlags.Soap) != 0) rate = Mathf.Max(rate, soap);
            return rate;
        }
    }
}