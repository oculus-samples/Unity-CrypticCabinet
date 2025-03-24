// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    /// Used to assist in selecting UI when distance grab interactors are present
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class UIRaycastUtility : MonoBehaviour
    {
        [SerializeField] private RayInteractor m_rayInteractor;

        [SerializeField] private DistanceGrabInteractor m_distanceGrabInteractor;

        // Start is called before the first frame update
        private void Start()
        {
            m_rayInteractor.WhenStateChanged += RayInteractorOnWhenStateChanged;
        }

        private void RayInteractorOnWhenStateChanged(InteractorStateChangeArgs obj)
        {
            m_distanceGrabInteractor.gameObject.SetActive(obj.NewState != InteractorState.Hover);
        }

        private void OnDestroy()
        {
            m_rayInteractor.WhenStateChanged -= RayInteractorOnWhenStateChanged;
        }
    }
}
