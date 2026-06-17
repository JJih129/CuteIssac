using System;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    /// <summary>
    /// 스테이지 진입 전에 풀에 미리 준비할 프레젠테이션 프리팹 수량입니다.
    /// 실제 스폰은 각 시스템이 담당하고, 이 데이터는 끊김 방지를 위한 준비값만 가집니다.
    /// </summary>
    [Serializable]
    public sealed class StagePrewarmProfile
    {
        [Header("Room Theme")]
        [SerializeField] [Min(0)] private int roomVisualPrewarmCount = 1;
        [SerializeField] [Min(0)] private int doorVisualPrewarmCount = 4;
        [SerializeField] [Min(0)] private int decorationPrewarmCount = 2;

        [Header("Gameplay Pools")]
        [Tooltip("스테이지 적 풀에 등록된 적 프리팹별 기본 프리워밍 수량입니다. 실제 웨이브 단위 추가 프리워밍은 RoomEnemySpawner가 이어서 처리합니다.")]
        [SerializeField] [Min(0)] private int enemyPrefabPrewarmCount = 1;
        [Tooltip("스테이지 보상 테이블에 등록된 픽업 프리팹별 기본 프리워밍 수량입니다. 실제 드랍 직전 추가 프리워밍은 RoomRewardSpawner가 이어서 처리합니다.")]
        [SerializeField] [Min(0)] private int rewardPickupPrewarmCount = 1;

        public int RoomVisualPrewarmCount => Mathf.Max(0, roomVisualPrewarmCount);
        public int DoorVisualPrewarmCount => Mathf.Max(0, doorVisualPrewarmCount);
        public int DecorationPrewarmCount => Mathf.Max(0, decorationPrewarmCount);
        public int EnemyPrefabPrewarmCount => Mathf.Max(0, enemyPrefabPrewarmCount);
        public int RewardPickupPrewarmCount => Mathf.Max(0, rewardPickupPrewarmCount);

        public static StagePrewarmProfile CreateDefault()
        {
            return new StagePrewarmProfile();
        }
    }
}
