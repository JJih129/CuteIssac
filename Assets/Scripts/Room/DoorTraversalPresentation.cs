using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Runtime-only traversal presentation for door departures and arrivals.
    /// It stays separate from authored door art so visuals can later be replaced without touching room navigation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoomDoor))]
    public sealed class DoorTraversalPresentation : MonoBehaviour
    {
        [Header("Departure")]
        [SerializeField] [Min(0.05f)] private float departureDuration = 0.22f;
        [SerializeField] [Min(0.05f)] private float guidedDepartureDuration = 0.34f;
        [SerializeField] [Min(0f)] private float departureBeamLength = 0.72f;
        [SerializeField] [Min(0f)] private float departureRingRadius = 0.88f;

        [Header("Arrival")]
        [SerializeField] [Min(0.05f)] private float arrivalDuration = 0.34f;
        [SerializeField] [Min(0.05f)] private float guidedArrivalDuration = 0.5f;
        [SerializeField] [Min(0f)] private float arrivalTrailLength = 0.9f;
        [SerializeField] [Min(0f)] private float arrivalRingRadius = 1.1f;

        private static Sprite s_WhiteSprite;
        private static Sprite s_CircleSprite;

        private RoomDoor _roomDoor;
        private Transform _visualRoot;
        private Transform _departureRoot;
        private Transform _arrivalRoot;
        private SpriteRenderer _departureRing;
        private SpriteRenderer _departureCore;
        private SpriteRenderer _departureBeam;
        private SpriteRenderer _arrivalRing;
        private SpriteRenderer _arrivalCore;
        private SpriteRenderer _arrivalTrail;
        private Vector3 _departureRingBaseScale = Vector3.one;
        private Vector3 _departureCoreBaseScale = Vector3.one;
        private Vector3 _departureBeamBaseScale = Vector3.one;
        private Vector3 _arrivalRingBaseScale = Vector3.one;
        private Vector3 _arrivalCoreBaseScale = Vector3.one;
        private Vector3 _arrivalTrailBaseScale = Vector3.one;
        private Color _departureAccent = Color.white;
        private Color _arrivalAccent = Color.white;
        private float _departureStartedAt = float.NegativeInfinity;
        private float _arrivalStartedAt = float.NegativeInfinity;
        private float _departureActiveDuration;
        private float _arrivalActiveDuration;
        private float _arrivalRoomWeight;

        private void Awake()
        {
            ResolveDoor();
            EnsureVisuals();
            SetVisualsActive(false, false);
        }

        private void OnEnable()
        {
            ResolveDoor();
            EnsureVisuals();
        }

        private void Update()
        {
            bool departureActive = UpdateDeparture();
            bool arrivalActive = UpdateArrival();
            SetVisualsActive(departureActive, arrivalActive);
        }

        public void PlayDeparturePulse(Color accentColor, RoomDirection direction, bool guided)
        {
            EnsureVisuals();
            _departureAccent = ResolveAccent(accentColor);
            _departureStartedAt = Time.unscaledTime;
            _departureActiveDuration = guided ? guidedDepartureDuration : departureDuration;
            _departureRoot.localPosition = Vector3.zero;
            _departureRoot.localRotation = Quaternion.Euler(0f, 0f, ResolveDirectionRotation(direction));
            _departureRoot.gameObject.SetActive(true);
            _visualRoot.gameObject.SetActive(true);
        }

        public void PlayArrivalSettle(Color accentColor, RoomDirection arrivalDoorDirection, bool guided, RoomType targetRoomType)
        {
            EnsureVisuals();
            _arrivalAccent = ResolveAccent(accentColor);
            _arrivalStartedAt = Time.unscaledTime;
            _arrivalRoomWeight = ResolveRoomWeight(targetRoomType);
            _arrivalActiveDuration = (guided ? guidedArrivalDuration : arrivalDuration) + (_arrivalRoomWeight * 0.08f);
            _arrivalRoot.localPosition = ResolveArrivalLocalPosition();
            _arrivalRoot.localRotation = Quaternion.Euler(0f, 0f, ResolveDirectionRotation(ResolveInwardDirection(arrivalDoorDirection)));
            _arrivalRoot.gameObject.SetActive(true);
            _visualRoot.gameObject.SetActive(true);
        }

        private bool UpdateDeparture()
        {
            if (_departureRoot == null || _departureActiveDuration <= 0f)
            {
                return false;
            }

            float elapsed = Time.unscaledTime - _departureStartedAt;
            if (elapsed < 0f || elapsed > _departureActiveDuration)
            {
                return false;
            }

            float normalized = Mathf.Clamp01(elapsed / _departureActiveDuration);
            float fade = 1f - normalized;
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            Color brightAccent = Color.Lerp(_departureAccent, Color.white, 0.3f);

            _departureRing.transform.localScale = _departureRingBaseScale * Mathf.Lerp(0.68f, 1.22f, normalized);
            _departureRing.color = new Color(_departureAccent.r, _departureAccent.g, _departureAccent.b, Mathf.Lerp(0.52f, 0f, normalized));

            _departureCore.transform.localScale = _departureCoreBaseScale * Mathf.Lerp(0.6f, 1.35f, pulse);
            _departureCore.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.66f, 0f, normalized));

            _departureBeam.transform.localScale = new Vector3(
                _departureBeamBaseScale.x * Mathf.Lerp(0.72f, 1.18f, pulse),
                _departureBeamBaseScale.y * Mathf.Lerp(0.42f, 1.08f, fade),
                1f);
            _departureBeam.transform.localPosition = new Vector3(0f, Mathf.Lerp(0.12f, departureBeamLength * 0.38f, pulse), 0f);
            _departureBeam.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.42f, 0f, normalized));
            return true;
        }

        private bool UpdateArrival()
        {
            if (_arrivalRoot == null || _arrivalActiveDuration <= 0f)
            {
                return false;
            }

            float elapsed = Time.unscaledTime - _arrivalStartedAt;
            if (elapsed < 0f || elapsed > _arrivalActiveDuration)
            {
                return false;
            }

            float normalized = Mathf.Clamp01(elapsed / _arrivalActiveDuration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            float roomScale = 1f + (_arrivalRoomWeight * 0.22f);
            Color brightAccent = Color.Lerp(_arrivalAccent, Color.white, 0.36f);

            _arrivalRoot.localPosition = ResolveArrivalLocalPosition();

            _arrivalRing.transform.localScale = _arrivalRingBaseScale * Mathf.Lerp(0.52f, 1.24f * roomScale, normalized);
            _arrivalRing.color = new Color(_arrivalAccent.r, _arrivalAccent.g, _arrivalAccent.b, Mathf.Lerp(0.64f, 0f, normalized));

            _arrivalCore.transform.localScale = _arrivalCoreBaseScale * Mathf.Lerp(0.7f, 1.22f, pulse);
            _arrivalCore.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.72f, 0f, normalized));

            _arrivalTrail.transform.localScale = new Vector3(
                _arrivalTrailBaseScale.x * Mathf.Lerp(0.82f, 1.14f, pulse),
                _arrivalTrailBaseScale.y * Mathf.Lerp(0.35f, 1.18f * roomScale, 1f - normalized),
                1f);
            _arrivalTrail.transform.localPosition = new Vector3(0f, -Mathf.Lerp(0.08f, arrivalTrailLength * 0.28f, 1f - normalized), 0f);
            _arrivalTrail.color = new Color(brightAccent.r, brightAccent.g, brightAccent.b, Mathf.Lerp(0.38f, 0f, normalized));
            return true;
        }

        private void ResolveDoor()
        {
            if (_roomDoor == null)
            {
                _roomDoor = GetComponent<RoomDoor>();
            }
        }

        private void EnsureVisuals()
        {
            if (_visualRoot != null)
            {
                return;
            }

            ResolveDoor();

            GameObject rootObject = new("DoorTraversalPresentation");
            rootObject.layer = gameObject.layer;
            _visualRoot = rootObject.transform;
            _visualRoot.SetParent(transform, false);
            _visualRoot.localPosition = Vector3.zero;

            _departureRoot = CreateRoot("DepartureRoot");
            _departureRing = CreatePart("DepartureRing", _departureRoot, GetCircleSprite(), Vector3.zero, new Vector3(departureRingRadius, departureRingRadius, 1f), new Color(1f, 1f, 1f, 0f), 44);
            _departureCore = CreatePart("DepartureCore", _departureRoot, GetCircleSprite(), Vector3.zero, new Vector3(0.18f, 0.18f, 1f), new Color(1f, 1f, 1f, 0f), 46);
            _departureBeam = CreatePart("DepartureBeam", _departureRoot, GetWhiteSprite(), new Vector3(0f, 0.18f, 0f), new Vector3(0.11f, departureBeamLength, 1f), new Color(1f, 1f, 1f, 0f), 45);

            _arrivalRoot = CreateRoot("ArrivalRoot");
            _arrivalRing = CreatePart("ArrivalRing", _arrivalRoot, GetCircleSprite(), Vector3.zero, new Vector3(arrivalRingRadius, arrivalRingRadius, 1f), new Color(1f, 1f, 1f, 0f), 43);
            _arrivalCore = CreatePart("ArrivalCore", _arrivalRoot, GetCircleSprite(), Vector3.zero, new Vector3(0.22f, 0.22f, 1f), new Color(1f, 1f, 1f, 0f), 46);
            _arrivalTrail = CreatePart("ArrivalTrail", _arrivalRoot, GetWhiteSprite(), new Vector3(0f, -0.16f, 0f), new Vector3(0.12f, arrivalTrailLength, 1f), new Color(1f, 1f, 1f, 0f), 42);

            _departureRingBaseScale = _departureRing.transform.localScale;
            _departureCoreBaseScale = _departureCore.transform.localScale;
            _departureBeamBaseScale = _departureBeam.transform.localScale;
            _arrivalRingBaseScale = _arrivalRing.transform.localScale;
            _arrivalCoreBaseScale = _arrivalCore.transform.localScale;
            _arrivalTrailBaseScale = _arrivalTrail.transform.localScale;
        }

        private Transform CreateRoot(string name)
        {
            GameObject rootObject = new(name);
            rootObject.layer = gameObject.layer;
            Transform root = rootObject.transform;
            root.SetParent(_visualRoot, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            root.gameObject.SetActive(false);
            return root;
        }

        private static SpriteRenderer CreatePart(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void SetVisualsActive(bool departureActive, bool arrivalActive)
        {
            if (_departureRoot != null)
            {
                _departureRoot.gameObject.SetActive(departureActive);
            }

            if (_arrivalRoot != null)
            {
                _arrivalRoot.gameObject.SetActive(arrivalActive);
            }

            if (_visualRoot != null)
            {
                _visualRoot.gameObject.SetActive(departureActive || arrivalActive);
            }
        }

        private Vector3 ResolveArrivalLocalPosition()
        {
            if (_roomDoor == null)
            {
                return Vector3.zero;
            }

            return transform.InverseTransformPoint(_roomDoor.GetArrivalPosition());
        }

        private static Color ResolveAccent(Color accentColor)
        {
            return accentColor.a > 0.01f
                ? new Color(accentColor.r, accentColor.g, accentColor.b, 1f)
                : new Color(1f, 0.86f, 0.42f, 1f);
        }

        private static float ResolveRoomWeight(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 1f,
                RoomType.Treasure => 0.82f,
                RoomType.Secret => 0.8f,
                RoomType.MiniBoss => 0.76f,
                RoomType.Challenge => 0.68f,
                RoomType.Shop => 0.56f,
                RoomType.Curse => 0.48f,
                RoomType.Trap => 0.34f,
                _ => 0f
            };
        }

        private static RoomDirection ResolveInwardDirection(RoomDirection doorDirection)
        {
            return doorDirection switch
            {
                RoomDirection.Up => RoomDirection.Down,
                RoomDirection.Right => RoomDirection.Left,
                RoomDirection.Down => RoomDirection.Up,
                RoomDirection.Left => RoomDirection.Right,
                _ => RoomDirection.Down
            };
        }

        private static float ResolveDirectionRotation(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => 0f,
                RoomDirection.Right => -90f,
                RoomDirection.Down => 180f,
                RoomDirection.Left => 90f,
                _ => 0f
            };
        }

        private static Sprite GetWhiteSprite()
        {
            if (s_WhiteSprite != null)
            {
                return s_WhiteSprite;
            }

            s_WhiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_WhiteSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null)
            {
                return s_CircleSprite;
            }

            const int size = 48;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeDoorTraversalCircle"
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - normalizedDistance);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            s_CircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_CircleSprite;
        }
    }
}
