namespace rlmg.Tools.Core
{
    using UnityEngine;
    using UnityEngine.Video;
    using rlmg.Tools.MediaPlayers;
    using System;
    using System.Collections;

    /// <summary>
    /// Manages a video player and its appearance with the AttractTimeout's Timeout and Dismiss methods.
    /// Includes the following features:
    /// 1. Fades a CanvasGroup to control the appearance of the video player, with configurable durations, and
    /// 2. Optionally start in attract state.
    /// </summary>
    [RequireComponent(typeof(AttractTimeout))]
    public class AttractVideoPlayer : VideoPlayerManager
    {
        protected AttractTimeout attractTimeout;

        /// <summary>
        /// Controls visibility of video player
        /// </summary>
        [Header("Attract Settings")]
        [SerializeField]
        protected CanvasGroup attractCanvasGroup;

        /// <summary>
        /// Whether or not to go into attract state once the video player has loaded
        /// </summary>
        [SerializeField]
        protected bool doGoToAttractOnLoad = true;

        /// <summary>
        /// Durations of fade in and fade out of canvas group visibility.
        /// </summary>
        /// <remarks>This is distinct from the base video player's fade duration, which are used for gracefully dipping to a color while the video is loading.</remarks>
        [SerializeField]
        protected float attractFadeInDuration = 1f, attractFadeOutDuration = 1f;

        /// <summary>
        /// Manages canvas group visibility fade
        /// </summary>
        protected Coroutine attractFadeRoutine;

        /// <summary>
        /// Stores the configured playback volume so fades can ramp back to it
        /// </summary>
        protected float savedVideoVolume = 1f;

        /// <summary>
        /// When the canvas group is fully visible
        /// </summary>
        public event Action AttractFadeInCompleted;

        /// <summary>
        /// When the canvas group is fully invisible
        /// </summary>
        public event Action AttractFadeOutCompleted;

        protected override void Awake()
        {
            attractTimeout = GetComponent<AttractTimeout>();

            base.Awake();

            attractTimeout.DoCount = false;
        }

        protected override void OnEnable()
        {
            if (VideoPlayer != null)
                VideoPlayer.prepareCompleted += OnVideoPlayerPrepared;

            attractTimeout.AttractStarting += OnAttractStarting;
            attractTimeout.AttractDismissed += OnAttractDismissed;

            base.OnEnable();
        }

        protected virtual void OnDisable()
        {
            if (VideoPlayer != null)
                VideoPlayer.prepareCompleted -= OnVideoPlayerPrepared;

            attractTimeout.AttractStarting -= OnAttractStarting;
            attractTimeout.AttractDismissed -= OnAttractDismissed;
        }

        protected void OnVideoPlayerPrepared(VideoPlayer vp)
        {
            // initialize saved volume from the current video player configuration
            savedVideoVolume = GetCurrentVideoVolume();

            attractTimeout.DoCount = true;

            if (doGoToAttractOnLoad)
                attractTimeout.ForceTimeout();
        }

        protected void OnAttractStarting()
        {
            FadeInAttract();
        }

        protected void OnAttractDismissed()
        {
            FadeOutAttract();
        }

        protected void FadeInAttract()
        {
            if (attractFadeRoutine != null)
                StopCoroutine(attractFadeRoutine);

            attractFadeRoutine = StartCoroutine(
                FadeInAttractRoutine());
        }

        protected void FadeOutAttract()
        {
            if (attractFadeRoutine != null)
                StopCoroutine(attractFadeRoutine);

            attractFadeRoutine = StartCoroutine(
                FadeOutAttractRoutine());
        }

        protected IEnumerator FadeInAttractRoutine()
        {
            if (VideoPlayer != null)
            {
                SetVideoVolume(0f);
                VideoPlayer.frame = 0;
                VideoPlayer.Play();
            }

            if (viewportImage != null)
                viewportImage.raycastTarget = true;

            yield return FadeCanvasGroupAndAudio(
                attractCanvasGroup,
                1f,
                attractFadeInDuration,
                true,
                0f,
                savedVideoVolume);

            AttractFadeInCompleted?.Invoke();

            attractFadeRoutine = null;
        }

        protected IEnumerator FadeOutAttractRoutine()
        {

            // ramp audio down to 0 while fading out
            float startVolume = GetCurrentVideoVolume();

            yield return FadeCanvasGroupAndAudio(
                attractCanvasGroup,
                0f,
                attractFadeOutDuration,
                true,
                startVolume,
                0f);

            if (VideoPlayer != null)
                VideoPlayer.Pause();

            if (viewportImage != null)
                viewportImage.raycastTarget = false;

            AttractFadeOutCompleted?.Invoke();

            attractFadeRoutine = null;
        }

        protected IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
        {
            // backward-compatible wrapper that does not touch audio
            yield return FadeCanvasGroupAndAudio(cg, targetAlpha, duration, false, 0f, 0f);
        }

        protected IEnumerator FadeCanvasGroupAndAudio(
            CanvasGroup cg,
            float targetAlpha,
            float duration,
            bool doAudio,
            float audioStart,
            float audioTarget)
        {
            if (cg == null && !doAudio)
                yield break;

            float startAlpha = cg != null ? cg.alpha : 0f;
            float t = 0f;

            // protect against zero duration
            if (duration <= 0f)
            {
                if (cg != null) cg.alpha = targetAlpha;
                if (doAudio) SetVideoVolume(audioTarget);
                yield break;
            }

            while (t < duration)
            {
                float u = t / duration;

                if (cg != null)
                    cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, u);

                if (doAudio)
                    SetVideoVolume(Mathf.Lerp(audioStart, audioTarget, u));

                t += Time.deltaTime;

                yield return null;
            }

            if (cg != null)
                cg.alpha = targetAlpha;

            if (doAudio)
                SetVideoVolume(audioTarget);
        }

        /// <summary>
        /// Read the current playback volume according to the VideoPlayer's audio output mode.
        /// Supports AudioSource and Direct audio output modes.
        /// </summary>
        protected float GetCurrentVideoVolume()
        {
            if (VideoPlayer == null ||
                !VideoPlayer.isPrepared)
                return savedVideoVolume;

            try
            {
                if (VideoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
                {
                    var src = VideoPlayer.GetTargetAudioSource((ushort)0);
                    if (src == null)
                    {
                        // fallback to any AudioSource on the same GameObject in case targets weren't set via API
                        src = VideoPlayer.GetComponent<AudioSource>();
                    }

                    if (src != null)
                        return src.volume;
                }
                else if (VideoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                {
                    return VideoPlayer.GetDirectAudioVolume(0);
                }
            }
            catch
            {
                // fall through to return saved value
            }

            return savedVideoVolume;
        }

        /// <summary>
        /// Set the playback volume according to the VideoPlayer's audio output mode.
        /// Supports AudioSource and Direct audio output modes.
        /// </summary>
        protected void SetVideoVolume(float volume)
        {
            if (VideoPlayer == null)
                return;

            try
            {
                if (VideoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
                {
                    var src = VideoPlayer.GetTargetAudioSource((ushort)0);

                    if (src == null)
                    {
                        src = VideoPlayer.GetComponent<AudioSource>();
                    }

                    if (src != null)
                        src.volume = volume;
                    else
                    {
                        // as a last resort, attempt direct audio
                        VideoPlayer.SetDirectAudioVolume((ushort)0, volume);
                    }
                }
                else if (VideoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                {
                    VideoPlayer.SetDirectAudioVolume((ushort)0, volume);
                }
            }
            catch
            {
                // ignore audio errors
            }
        }
    }

}