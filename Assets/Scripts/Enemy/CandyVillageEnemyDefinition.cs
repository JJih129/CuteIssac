using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Designer-facing marker for mapping planned Candy enemies onto existing enemy prefabs.
    /// It does not drive AI; it documents the intended base prefab and validates required components.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class CandyVillageEnemyDefinition : MonoBehaviour
    {
        public enum CandyVillageEnemyKind
        {
            GingerbreadCookie = 0,
            MacaronCat = 1,
            PuddingJellyfish = 2,
            CandyBat = 3,
            JellyBear = 4
        }

        public enum StageGroup
        {
            CandyVillage = 0,
            CandyForest = 1
        }

        public enum AttackType
        {
            MeleeContact = 0,
            DashContact = 1,
            RangedProjectile = 2
        }

        public enum RecommendedBasePrefab
        {
            EnemyChaser = 0,
            EnemyDasher = 1,
            EnemyShooter = 2
        }

        [Header("Plan Mapping")]
        [SerializeField] private CandyVillageEnemyKind enemyKind = CandyVillageEnemyKind.GingerbreadCookie;
        [SerializeField] private string enemyDisplayName = "Gingerbread Cookie";
        [SerializeField] private string localizedDisplayName = "\uC9C4\uC800\uBE0C\uB808\uB4DC \uCFE0\uD0A4";
        [SerializeField] private StageGroup stageGroup = StageGroup.CandyVillage;
        [SerializeField] private AttackType attackType = AttackType.MeleeContact;
        [SerializeField] private RecommendedBasePrefab recommendedBasePrefab = RecommendedBasePrefab.EnemyChaser;

        [Header("Horror Phase")]
        [SerializeField] private bool usesHorrorPhase = true;
        [SerializeField] [Range(0.01f, 1f)] private float horrorThreshold = 0.5f;

        [Header("Validation")]
        [SerializeField] private bool logValidationWarnings;

        private EnemyController _enemyController;
        private EnemyHorrorPhaseController _horrorPhaseController;
        private ChaserEnemyBrain _chaserBrain;
        private MacaronChaserBrain _macaronChaserBrain;
        private DasherEnemyBrain _dasherBrain;
        private ShooterEnemyBrain _shooterBrain;
        private CandyBatShooterBrain _candyBatShooterBrain;

        public CandyVillageEnemyKind EnemyKind => enemyKind;
        public string EnemyDisplayName => enemyDisplayName;
        public string LocalizedDisplayName => localizedDisplayName;
        public StageGroup SpawnStageGroup => stageGroup;
        public AttackType EnemyAttackType => attackType;
        public RecommendedBasePrefab BasePrefabRecommendation => recommendedBasePrefab;
        public bool UsesHorrorPhase => usesHorrorPhase;
        public float HorrorThreshold => horrorThreshold;

        private void Awake()
        {
            ResolveReferences();
        }

        public bool HasExpectedBrain()
        {
            ResolveReferences();

            return recommendedBasePrefab switch
            {
                RecommendedBasePrefab.EnemyChaser => _chaserBrain != null || _macaronChaserBrain != null,
                RecommendedBasePrefab.EnemyDasher => _dasherBrain != null,
                RecommendedBasePrefab.EnemyShooter => _shooterBrain != null || _candyBatShooterBrain != null,
                _ => false
            };
        }

        public bool HasRequiredHorrorController()
        {
            ResolveReferences();
            return !usesHorrorPhase || _horrorPhaseController != null;
        }

        public void ApplyGingerbreadDefaults()
        {
            enemyKind = CandyVillageEnemyKind.GingerbreadCookie;
            enemyDisplayName = "Gingerbread Cookie";
            localizedDisplayName = "\uC9C4\uC800\uBE0C\uB808\uB4DC \uCFE0\uD0A4";
            stageGroup = StageGroup.CandyVillage;
            attackType = AttackType.MeleeContact;
            recommendedBasePrefab = RecommendedBasePrefab.EnemyChaser;
            usesHorrorPhase = true;
            horrorThreshold = 0.5f;
        }

        public void ApplyMacaronCatDefaults()
        {
            enemyKind = CandyVillageEnemyKind.MacaronCat;
            enemyDisplayName = "Macaron Cat";
            localizedDisplayName = "\uB9C8\uCE74\uB871 \uB0E5";
            stageGroup = StageGroup.CandyVillage;
            attackType = AttackType.MeleeContact;
            recommendedBasePrefab = RecommendedBasePrefab.EnemyChaser;
            usesHorrorPhase = true;
            horrorThreshold = 0.5f;
        }

        public void ApplyPuddingJellyfishDefaults()
        {
            enemyKind = CandyVillageEnemyKind.PuddingJellyfish;
            enemyDisplayName = "Pudding Jellyfish";
            localizedDisplayName = "\uD478\uB529 \uD574\uD30C\uB9AC";
            stageGroup = StageGroup.CandyVillage;
            attackType = AttackType.RangedProjectile;
            recommendedBasePrefab = RecommendedBasePrefab.EnemyShooter;
            usesHorrorPhase = true;
            horrorThreshold = 0.5f;
        }

        public void ApplyCandyBatDefaults()
        {
            enemyKind = CandyVillageEnemyKind.CandyBat;
            enemyDisplayName = "Candy Bat";
            localizedDisplayName = "\uC54C\uC0AC\uD0D5 \uBC15\uC950";
            stageGroup = StageGroup.CandyForest;
            attackType = AttackType.RangedProjectile;
            recommendedBasePrefab = RecommendedBasePrefab.EnemyShooter;
            usesHorrorPhase = true;
            horrorThreshold = 0.5f;
        }

        public void ApplyJellyBearDefaults()
        {
            enemyKind = CandyVillageEnemyKind.JellyBear;
            enemyDisplayName = "Jelly Bear";
            localizedDisplayName = "\uC824\uB9AC \uBCA0\uC5B4";
            stageGroup = StageGroup.CandyVillage;
            attackType = AttackType.DashContact;
            recommendedBasePrefab = RecommendedBasePrefab.EnemyDasher;
            usesHorrorPhase = true;
            horrorThreshold = 0.5f;
        }

        [ContextMenu("Apply Gingerbread Cookie Defaults")]
        private void ApplyGingerbreadDefaultsFromMenu()
        {
            ApplyGingerbreadDefaults();
            ResolveReferences();
        }

        [ContextMenu("Apply Macaron Cat Defaults")]
        private void ApplyMacaronCatDefaultsFromMenu()
        {
            ApplyMacaronCatDefaults();
            ResolveReferences();
        }

        [ContextMenu("Apply Pudding Jellyfish Defaults")]
        private void ApplyPuddingJellyfishDefaultsFromMenu()
        {
            ApplyPuddingJellyfishDefaults();
            ResolveReferences();
        }

        [ContextMenu("Apply Candy Bat Defaults")]
        private void ApplyCandyBatDefaultsFromMenu()
        {
            ApplyCandyBatDefaults();
            ResolveReferences();
        }

        [ContextMenu("Apply Jelly Bear Defaults")]
        private void ApplyJellyBearDefaultsFromMenu()
        {
            ApplyJellyBearDefaults();
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (_enemyController == null)
            {
                _enemyController = GetComponent<EnemyController>();
            }

            if (_horrorPhaseController == null)
            {
                _horrorPhaseController = GetComponent<EnemyHorrorPhaseController>();
            }

            if (_chaserBrain == null)
            {
                _chaserBrain = GetComponent<ChaserEnemyBrain>();
            }

            if (_dasherBrain == null)
            {
                _dasherBrain = GetComponent<DasherEnemyBrain>();
            }

            if (_macaronChaserBrain == null)
            {
                _macaronChaserBrain = GetComponent<MacaronChaserBrain>();
            }

            if (_shooterBrain == null)
            {
                _shooterBrain = GetComponent<ShooterEnemyBrain>();
            }

            if (_candyBatShooterBrain == null)
            {
                _candyBatShooterBrain = GetComponent<CandyBatShooterBrain>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
            ApplyDefaultsForDetectedBrain();
        }

        private void OnValidate()
        {
            ResolveReferences();
            horrorThreshold = Mathf.Clamp(horrorThreshold, 0.01f, 1f);

            if (!logValidationWarnings)
            {
                return;
            }

            if (!HasExpectedBrain())
            {
                Debug.LogWarning($"{nameof(CandyVillageEnemyDefinition)} '{enemyDisplayName}' expects {recommendedBasePrefab}, but the matching brain is missing.", this);
            }

            if (usesHorrorPhase && _horrorPhaseController == null)
            {
                Debug.LogWarning($"{nameof(CandyVillageEnemyDefinition)} '{enemyDisplayName}' uses horror phase but EnemyHorrorPhaseController is missing.", this);
            }
        }

        private void ApplyDefaultsForDetectedBrain()
        {
            if (_macaronChaserBrain != null)
            {
                ApplyMacaronCatDefaults();
                return;
            }

            if (_dasherBrain != null)
            {
                ApplyMacaronCatDefaults();
                return;
            }

            if (_shooterBrain != null)
            {
                ApplyPuddingJellyfishDefaults();
                return;
            }

            ApplyGingerbreadDefaults();
        }
    }
}
