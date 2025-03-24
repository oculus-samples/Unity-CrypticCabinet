// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Interactions
{
    /// <summary>
    ///     Defines a snap zone onto which a screwable object can snap via a screwing-in motion.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(Rigidbody))]
    public class ScrewSnapZone : MonoBehaviour
    {
        public Vector3 GuideBottomPosition => transform.position;

        public Vector3 GuideTopPosition => GuideBottomPosition + transform.up * ScrewHeight;

        public float ScrewHeight = 0.1f;
        public bool HasObject => CurrentObject;

        [HideInInspector] public ScrewableObject CurrentObject;
        public UnityEvent<ScrewableObject> OnObjectSnap;
        public UnityEvent<ScrewableObject> OnObjectCompleteScrew;
        public UnityEvent<ScrewableObject> OnObjectStartUnscrew;
        public UnityEvent<ScrewableObject> OnObjectRemoved;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(GuideBottomPosition, GuideTopPosition);
        }
    }
}
