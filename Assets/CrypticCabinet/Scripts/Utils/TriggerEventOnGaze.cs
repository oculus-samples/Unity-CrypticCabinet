// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Utils
{
    [MetaCodeSample("CrypticCabinet")]
    public class TriggerEventOnGaze : NetworkBehaviour
    {
        [SerializeField] private Transform m_usersHead;
        [SerializeField] private float m_maxAngularDistance = 20f;
        [SerializeField] private UnityEvent m_onTrigger;
        private bool m_hasTriggered;

        private void Start()
        {
            if (m_usersHead == null)
            {
                m_usersHead = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (m_hasTriggered)
            {
                return;
            }

            var usersHeadPosition = m_usersHead.position;

            var nearestCabinetPos = MathsUtils.NearestPointOnLine(transform.position, Vector3.up, usersHeadPosition);
            var heading = nearestCabinetPos - usersHeadPosition;
            var angularDistance = Vector3.SignedAngle(m_usersHead.forward, heading, Vector3.up);

            if (Mathf.Abs(angularDistance) < m_maxAngularDistance)
            {
                FireEventRpc();
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void FireEventRpc()
        {
            if (m_hasTriggered)
            {
                return;
            }

            m_onTrigger?.Invoke();
            m_hasTriggered = true;
        }
    }
}