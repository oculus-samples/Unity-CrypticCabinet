// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     Interface defining the core functions for a space finder object.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public interface ISpaceFinder
    {
        public bool HasSetUpCompleted();
        public void CleanUp();
        public void GetFinderTransform(out Matrix4x4 localToWorldMatrix);
        public void GetFinderSize(out Vector3 worldSize);
        public bool RequestRandomLocation(Vector3 locationToFace, float objectRadius, out Vector3 foundPosition, out Quaternion foundRotation, bool markAsBlocked = false, float edgeDistance = -1);
        public bool RequestRandomLocation(Vector3 locationToFace, Vector3 objectDimensions, out Vector3 foundPosition, out Quaternion foundRotation, bool markAsBlocked = false);
        public void CalculateBlockedArea(Matrix4x4 objectTransform, Vector2 objectSize);
        public void CalculateBlockedArea(Matrix4x4 objectTransform, Vector3 objectSize);
        public void RememberDistanceField();
        public void ResetDistanceField();
        public void SetDebugViewEnabled(bool isVisible);
        public void ToggleDebugView();
        public void SetSearchCollidersActive(bool collidersActive);
        public void RequestTrulyRandomLocation(out Vector3 position);
    }
}