using UnityEngine;

namespace CuteIssac.Core.Audio
{
    public readonly struct AudioPlaybackRequest
    {
        public AudioPlaybackRequest(
            GameAudioEventType eventType,
            Vector3 position,
            bool useWorldPosition,
            float volumeScale = 1f,
            float pitchScale = 1f)
        {
            EventType = eventType;
            Position = position;
            UseWorldPosition = useWorldPosition;
            VolumeScale = volumeScale;
            PitchScale = pitchScale;
        }

        public GameAudioEventType EventType { get; }
        public Vector3 Position { get; }
        public bool UseWorldPosition { get; }
        public float VolumeScale { get; }
        public float PitchScale { get; }
    }
}
