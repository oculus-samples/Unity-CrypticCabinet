// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Puzzles.SandPuzzle
{
    /// <summary>
    ///     Limitation: Every hookable work only with one hook in the same "trigger zone"
    ///     If two hooks are in the same trigger zone of the hookable, the behavior is undefined.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    [RequireComponent(typeof(Collider), typeof(PointableUnityEventWrapper), typeof(Rigidbody))]
    public class Hookable : NetworkBehaviour
    {
        [SerializeField] private Vector3 m_anchorPosition;
        [SerializeField] private Vector3 m_hingeAxis = new(0f, 0f, 1f);
        [SerializeField] private SandBucket m_sandBucket;

        private Collider m_triggerCollider;
        private Hook m_hook;
        private Hook m_lastHook;
        private bool m_isHookInTriggerZone;
        private HingeJoint m_hookableHinge;
        [SerializeField] private Transform m_hookTargetLocation;
        [SerializeField] private float m_forceSnapDistance = 0.5f;
        private List<Collider> m_collisionColliders = new();

        private void Start()
        {
            var handGrabInteractable = GetComponent<PointableUnityEventWrapper>();
            handGrabInteractable.WhenSelect.AddListener(ObjectGrabbed);
            handGrabInteractable.WhenUnselect.AddListener(ObjectReleased);

            var colliders = GetComponents<Collider>();
            foreach (var colliderIt in colliders)
            {
                if (colliderIt.isTrigger)
                {
                    m_triggerCollider = colliderIt;
                }
                else
                {
                    m_collisionColliders.Add(colliderIt);
                }
            }

            if (m_triggerCollider == null)
            {
                Debug.LogError("No trigger collider for the hook to detect hooking!");
            }
        }

        private void ObjectGrabbed(PointerEvent arg)
        {
            ObjectGrabbedRpc();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void ObjectGrabbedRpc()
        {
            var hinge = GetComponent<HingeJoint>();
            if (hinge != null)
            {
                hinge.connectedBody = null;
                Destroy(hinge);
            }

            if (m_lastHook != null)
            {
                m_lastHook.OnGameObjectRemoved?.Invoke(gameObject);
                m_lastHook = null;
            }

            m_hook = null;
        }

        private void ObjectReleased(PointerEvent arg)
        {
            ObjectReleasedRpc();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void ObjectReleasedRpc()
        {
            if (!m_isHookInTriggerZone || m_hook == null)
            {
                var hooks = FindObjectsOfType<Hook>().ToList();
                if (hooks.Count > 0)
                {
                    var thisPos = transform.position;
                    hooks = hooks.OrderBy(hook => Vector3.Distance(thisPos, hook.transform.position)).ToList();

                    if (Vector3.Distance(thisPos, hooks[0].transform.position) < m_forceSnapDistance)
                    {
                        m_hook = hooks[0];
                        m_isHookInTriggerZone = true;
                    }
                    else
                    {
                        m_sandBucket.SetIsHooked(false);
                        SetCollisionColliders(true);
                    }
                }
                else
                {
                    m_sandBucket.SetIsHooked(false);
                    SetCollisionColliders(true);
                }
            }

            if (m_isHookInTriggerZone && m_hook != null)
            {
                var rigidbodyFollower = m_hook.GetComponent<RigidbodyFollower>();
                rigidbodyFollower.JumpToLocation(m_hookTargetLocation.position);

                var hookRb = m_hook.GetComponent<Rigidbody>();
                m_hookableHinge = GetComponent<HingeJoint>();

                if (m_hookableHinge == null)
                {
                    m_hookableHinge = gameObject.AddComponent<HingeJoint>();
                }

                // Rotate hinge angle so that the bucket handle is perpendicular to hook
                m_hookableHinge.transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);
                m_hookableHinge.autoConfigureConnectedAnchor = false;
                m_hookableHinge.axis = m_hingeAxis;
                m_hookableHinge.anchor = m_anchorPosition;
                m_hookableHinge.useLimits = true;
                m_hookableHinge.extendedLimits = true;
                var jointLimits = m_hookableHinge.limits;
                jointLimits.min = -20.0f;
                jointLimits.max = 20.0f;
                m_hookableHinge.limits = jointLimits;
                m_hookableHinge.connectedAnchor = m_hook.transform.position;
                m_hookableHinge.useSpring = true;
                var hookableHingeSpring = m_hookableHinge.spring;
                hookableHingeSpring.spring = 10;
                hookableHingeSpring.targetPosition = 0.0f;
                m_hookableHinge.spring = hookableHingeSpring;

                _ = StartCoroutine(WaitForFixedUpdate(hookRb));

                m_hook.OnGameObjectAttached?.Invoke(gameObject);
                m_sandBucket.SetIsHooked(true);
                SetCollisionColliders(false);
                m_lastHook = m_hook;
            }
            else
            {
                m_sandBucket.SetIsHooked(false);
                SetCollisionColliders(true);
            }
        }

        private IEnumerator WaitForFixedUpdate(Rigidbody hookRb)
        {
            yield return new WaitForFixedUpdate();
            m_hookableHinge.connectedBody = hookRb;
            m_hookableHinge.connectedAnchor = Vector3.zero;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.TryGetComponent<Hook>(out var otherHook))
            {
                return;
            }

            if (otherHook is null)
            {
                return;
            }

            if (otherHook == default)
            {
                return;
            }

            m_hook = otherHook;
            m_isHookInTriggerZone = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.isTrigger)
            {
                return;
            }

            if (!other.gameObject.TryGetComponent<Hook>(out var otherHook))
            {
                return;
            }

            if (otherHook is null)
            {
                return;
            }

            if (otherHook == default)
            {
                return;
            }

            m_hook = null;
            m_isHookInTriggerZone = false;
        }

        private void SetCollisionColliders(bool collisionEnabled)
        {
            foreach (var c in m_collisionColliders)
            {
                c.isTrigger = !collisionEnabled;
            }
        }
    }
}
