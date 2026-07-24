namespace rlmg.Tools.Core
{
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// For subclassing or event listening to trigger an attract state after a configurable period of no user input
    /// </summary>
    public class AttractTimeout : MonoBehaviour
    {
        /// <summary>
        /// Whether or not the timeout should be counted towards
        /// </summary>
        public bool DoCount = false;

        /// <summary>
        /// How many seconds of no user input before the attract state is triggered
        /// </summary>
        public int TimeoutDuration = 60;

        /// <summary>
        /// Managed by this class monitoring user input
        /// </summary>
        protected bool anyClick = false;

        /// <summary>
        /// Managed by this class monitoring user input
        /// </summary>
        public float TimeSinceLastClick = 0.0f;

        /// <summary>
        /// Managed by this class monitoring user input and timeout state
        /// </summary>
        protected bool timedOut = false;

        /// <summary>
        /// Invoked when the attract state has begun
        /// </summary>
        public event Action AttractStarting;

        /// <summary>
        /// Invoked after user input is detected, when the attract state is ending
        /// </summary>
        public event Action AttractDismissed;

        protected virtual void Update()
        {
            if (!DoCount)
            {
                return;
            }

            TimeSinceLastClick += Time.deltaTime;

            if (Keyboard.current.anyKey.wasPressedThisFrame ||
                //Mouse.current.press.wasPressedThisFrame ||
                Pointer.current.press.wasPressedThisFrame ||
                Mouse.current.scroll.y.value > 0)
            {
                anyClick = true;
            }

            if (anyClick)
            {
                TimeSinceLastClick = 0.0f;
                anyClick = false;
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                Timeout();
            }


            if (TimeSinceLastClick > TimeoutDuration && !timedOut)
            {
                Timeout();
            }
            else if (TimeSinceLastClick < TimeoutDuration && timedOut)
            {
                Dismiss();
            }
        }

        /// <summary>
        /// Do essential timeout / attract starting logic when the a period of time greater than the TimeoutDuration has passed without user input
        /// </summary>
        protected void Timeout()
        {
            TimeSinceLastClick = TimeoutDuration + 1; //allows Timeout to be invoked to force a timeout
            timedOut = true;

            OnTimeout();

            AttractStarting?.Invoke();
        }

        /// <summary>
        /// Do essential dismiss / attract ending logic when user input is detected after a timeout has already happened
        /// </summary>
        protected void Dismiss()
        {
            TimeSinceLastClick = 0f; //allows Dismiss to be invoked to force a dismissal
            timedOut = false;

            OnDismiss();

            AttractDismissed?.Invoke();
        }

        /// <summary>
        /// When the a period of time greater than the TimeoutDuration has passed without user input
        /// </summary>
        protected virtual void OnTimeout() { }

        /// <summary>
        /// When user input is detected after a timeout
        /// </summary>
        protected virtual void OnDismiss() { }

        /// <summary>
        /// Start the timeout timer over
        /// </summary>
        public void ResetTimer()
        {
            TimeSinceLastClick = 0f;
        }

        /// <summary>
        /// Enter a timed out state immediately
        /// </summary>
        public virtual void ForceTimeout()
        {
            Timeout();
        }

        /// <summary>
        /// Enter a dismissed state immedidately
        /// </summary>
        public virtual void ForceDismiss()
        {
            Dismiss();
        }
    }
}

