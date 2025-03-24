// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Force the snapping of a specified object onto a target snap zone.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ForceSnap : MonoBehaviour
    {
        [Serializable]
        public struct SnapPair
        {
            public SnapInteractor ObjectToSnap;
            public SnapInteractable TargetSnapZone;
        }

        [SerializeField] private List<SnapPair> m_snapPairs = new();

        public void DoSnaps()
        {
            _ = StartCoroutine(nameof(DoSnapAsync));
        }

        private IEnumerator DoSnapAsync()
        {
            foreach (var pair in m_snapPairs)
            {
                pair.TargetSnapZone.AddSelectingInteractor(pair.ObjectToSnap);
                pair.ObjectToSnap.SetComputeCandidateOverride(() => pair.TargetSnapZone);
                pair.ObjectToSnap.ProcessCandidate();
            }

            yield return null;

            foreach (var pair in m_snapPairs)
            {
                pair.ObjectToSnap.Select();
            }
        }
    }
}