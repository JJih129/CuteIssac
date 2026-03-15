using System;
using UnityEngine;

namespace CuteIssac.Core.Audio
{
    /// <summary>
    /// Gameplay systems raise audio requests here.
    /// The audio system resolves cue assets and playback behavior centrally.
    /// </summary>
    public static class GameAudioEvents
    {
        public static event Action<AudioPlaybackRequest> Requested;

        public static void Raise(
            GameAudioEventType eventType,
            Vector3 position,
            bool useWorldPosition = true,
            float volumeScale = 1f,
            float pitchScale = 1f)
        {
            Requested?.Invoke(new AudioPlaybackRequest(eventType, position, useWorldPosition, volumeScale, pitchScale));
        }

        public static void RaiseUi(GameAudioEventType eventType, float volumeScale = 1f, float pitchScale = 1f)
        {
            Requested?.Invoke(new AudioPlaybackRequest(eventType, Vector3.zero, false, volumeScale, pitchScale));
        }
    }
}
