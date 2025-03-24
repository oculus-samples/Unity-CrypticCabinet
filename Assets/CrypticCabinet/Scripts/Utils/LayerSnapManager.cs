// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Defines the rules to allow the snapping of objects belonging to the expected snap layer.
    ///     This is to only allow the snap of snappable objects to the expected snap areas.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(Rigidbody))]
    public class LayerSnapManager : MonoBehaviour
    {
        [SerializeField] private SnapInteractor m_snapper;
        [SerializeField] private LayerMask m_snapLayers;
        [SerializeField] private Grabbable m_grabbable;

        private bool m_canSnap;
        private bool m_held;
        private NetworkObject m_networkObject;

        private void Start()
        {
            m_networkObject = GetComponentInParent<NetworkObject>();

            m_grabbable.WhenPointerEventRaised += obj =>
            {
                if (obj.Type == PointerEventType.Select)
                {
                    if (obj.Data is GrabInteractor or HandGrabInteractor)
                    {
                        m_held = true;
                    }
                }
                else if (obj is { Type: PointerEventType.Unselect, Data: GrabInteractor or HandGrabInteractor })
                {
                    m_held = false;
                }
            };

            m_snapper.SetComputeShouldSelectOverride(ComputeShouldSelectOverride, false);
        }

        public void ResetOverrideCheck()
        {
            m_snapper.SetComputeShouldSelectOverride(ComputeShouldSelectOverride, false);
        }

        private bool ComputeShouldSelectOverride()
        {
            return m_canSnap && !m_held && (m_networkObject == null || m_networkObject.HasStateAuthority);
        }

        private void OnTriggerEnter(Collider other)
        {
            SetStay(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            SetStay(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            SetLeave(other.gameObject);
        }

        private void OnCollisionEnter(Collision other)
        {
            SetStay(other.gameObject);
        }

        private void OnCollisionStay(Collision other)
        {
            SetStay(other.gameObject);
        }

        private void OnCollisionExit(Collision other)
        {
            SetLeave(other.gameObject);
        }

        private void SetStay(GameObject other)
        {
            if (((1 << other.layer) & m_snapLayers.value) != 0)
            {
                m_canSnap = true;
            }
        }

        private void SetLeave(GameObject other)
        {
            if (((1 << other.layer) & m_snapLayers.value) != 0)
            {
                m_canSnap = false;
            }
        }
    }
}