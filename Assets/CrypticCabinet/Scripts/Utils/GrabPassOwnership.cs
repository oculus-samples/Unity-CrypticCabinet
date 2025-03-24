// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Establishes the state authority ownership transfer to grant a grabbing action.
    ///     This is to guarantee that whenever a player tries to grab an object, that player will be the only
    ///     one able to grab that object assuming the state authority was acquired.
    ///     When another player tries to grab an object, the state authority change is requested before allowing
    ///     the grab of the object for that other player.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(Grabbable))]
    public class GrabPassOwnership : NetworkBehaviour, IStateAuthorityChanged
    {
        private Grabbable m_grabbable;
        private NetworkObject m_networkObject;
        [SerializeField] private Renderer m_debugRenderer;
        [SerializeField] private bool m_changeColorForAuthority;
        [SerializeField] private Collider m_grabbableCollider;
        [SerializeField] private Collider[] m_additionalGrabbableColliders;
        [SerializeField] private bool m_shouldDisableOnGrab = true;
        [SerializeField] private bool m_shouldEnableOnRelease = true;
        [SerializeField] private Rigidbody m_rigidbody;
        private bool m_originalKinematicState;


        private void Start()
        {
            if (m_rigidbody == null)
            {
                m_rigidbody = GetComponent<Rigidbody>();
            }

            m_grabbable = GetComponent<Grabbable>();
            m_networkObject = GetComponent<NetworkObject>();
            if (m_networkObject == null)
            {
                m_networkObject = GetComponentInParent<NetworkObject>();
            }

            if (m_grabbableCollider == null)
            {
                m_grabbableCollider = GetComponentInChildren<Collider>();
            }

            m_grabbable.WhenPointerEventRaised += GrabbableOnWhenPointerEventRaised;
        }

        private void OnDestroy()
        {
            if (m_grabbable != null)
            {
                m_grabbable.WhenPointerEventRaised -= GrabbableOnWhenPointerEventRaised;
            }
        }

        public override void Spawned()
        {
            base.Spawned();

            if (m_changeColorForAuthority && m_debugRenderer != null && m_debugRenderer.material != null)
            {
                m_debugRenderer.material.color = HasStateAuthority ? Color.green : Color.red;
            }
        }

        public void StateAuthorityChanged()
        {
            if (m_changeColorForAuthority && m_debugRenderer != null && m_debugRenderer.material != null)
            {
                m_debugRenderer.material.color = HasStateAuthority ? Color.green : Color.red;
            }
        }

        private void GrabbableOnWhenPointerEventRaised(PointerEvent obj)
        {
            if (obj.Type == PointerEventType.Select)
            {
                if (obj.Data is GrabInteractor or HandGrabInteractor or TouchHandGrabInteractor)
                {
                    m_networkObject.RequestStateAuthority();
                    if (m_shouldDisableOnGrab)
                    {
                        RpcBeingHeld(true);
                    }
                }
            }
            else if (obj.Type == PointerEventType.Unselect)
            {
                if (obj.Data is GrabInteractor or HandGrabInteractor or TouchHandGrabInteractor)
                {
                    if (m_shouldEnableOnRelease)
                    {
                        RpcBeingHeld(false);
                    }
                }
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void RpcBeingHeld(bool held, RpcInfo info = default)
        {
            if (info.Source == PlayerRef.None || info.Source.PlayerId == Runner.LocalPlayer.PlayerId)
            {
                return;
            }

            m_grabbableCollider.enabled = !held;

            if (m_additionalGrabbableColliders != null)
            {
                foreach (var c in m_additionalGrabbableColliders)
                {
                    if (c != null)
                    {
                        c.enabled = !held;
                    }
                }
            }

            if (m_rigidbody == null)
            {
                return;
            }

            if (held)
            {
                m_originalKinematicState = m_rigidbody.isKinematic;
                m_rigidbody.isKinematic = true;
            }
            else
            {
                m_rigidbody.isKinematic = m_originalKinematicState;
            }
        }
    }
}