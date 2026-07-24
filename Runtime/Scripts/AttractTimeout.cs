namespace rlmg.Tools.Core
{
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;
    public class AttractTimeout : MonoBehaviour
    {
        public bool DoCount = false;

        /// <summary>
        /// Seconds
        /// </summary>
        public int TimeoutDuration = 60;

        protected bool anyClick = false;

        public float TimeSinceLastClick = 0.0f;

        protected bool timedOut = false;

        public event Action AttractStarting;

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
        /// When the a period of time greater than the TimeoutDuration has passed without user input
        /// </summary>
        protected virtual void Timeout()
        {
            TimeSinceLastClick = TimeoutDuration + 1; //allows Timeout to be invoked to force a timeout
            timedOut = true;

            AttractStarting?.Invoke();
        }

        /// <summary>
        /// When user input is detected after a timeout
        /// </summary>

        protected virtual void Dismiss()
        {
            TimeSinceLastClick = 0f; //allows Dismiss to be invoked to force a dismissal
            timedOut = false;

            AttractDismissed?.Invoke();
        }

        public void ResetTimer()
        {
            TimeSinceLastClick = 0f;
        }

        public void ForceTimeout()
        {
            Timeout();
        }
    }
}

