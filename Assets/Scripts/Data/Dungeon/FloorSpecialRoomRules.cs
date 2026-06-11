using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    [CreateAssetMenu(fileName = "FloorSpecialRoomRules", menuName = "CuteIssac/Data/Dungeon/Floor Special Room Rules")]
    public sealed class FloorSpecialRoomRules : ScriptableObject
    {
        [SerializeField] private List<SpecialRoomRuleData> rules = new();

        public IReadOnlyList<SpecialRoomRuleData> Rules => rules;

        public void CollectAvailableRules(int floorIndex, List<SpecialRoomRuleData> results)
        {
            if (results == null)
            {
                return;
            }

            for (int index = 0; index < rules.Count; index++)
            {
                SpecialRoomRuleData rule = rules[index];
                if (rule != null && rule.IsAvailableForFloor(floorIndex))
                {
                    results.Add(rule);
                }
            }
        }
    }
}
