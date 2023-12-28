// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Moves the transform of this object following the trajectory of the desired Quest controller.
    ///     Stops the move when the desired trigger is pressed.
    /// </summary>
    public class MoveTransformWithController : MonoBehaviour
    {
        /// <summary>
        ///     If enabled, this object will follow the raycast of the specified Quest controller.
        /// </summary>
        [SerializeField] private bool m_isMoving;
        [SerializeField] private bool m_isAttachedToFloor;
        [SerializeField] private Transform m_rootTransform;

        /// <summary>
        /// If true, the next phase of the game will be triggered after the objects has been placed.
        /// </summary>
        [SerializeField] private bool m_goNextGamePhaseAfterPlacement;

        private const float MIN_FLOOR_Y_THRESHOLD = 0.1f;
        private const float MIN_CEILING_Y_THRESHOLD = 1.6f;

        private RayInteractorCursorVisual m_rayInteractorCursor;

        /// <summary>
        ///     The trigger that will cause the move to stop.
        /// </summary>
        [SerializeField] private OVRInput.RawButton m_stopMoveTrigger;

        private void Start()
        {
            // Get the root game objects in the current scene
            var currentScene = SceneManager.GetActiveScene();
            var rootObjects = currentScene.GetRootGameObjects();

            // Iterate through the root game objects
            foreach (var rootObject in rootObjects)
            {
                // Try to get the RayInteractor component
                m_rayInteractorCursor = rootObject.GetComponentInChildren<RayInteractorCursorVisual>();

                // If found, break the loop
                if (m_rayInteractorCursor != null)
                    break;
            }

            // Log an error if the RayInteractor component was not found
            if (m_rayInteractorCursor == null)
                Debug.LogError("RayInteractor not found in the scene.");
        }

        private void Update()
        {
            if (!m_isMoving)
            {
                return;
            }

            // Get the Ray Interactor from the scene
            if (m_rayInteractorCursor == null)
            {
                Debug.LogError("RayInteractor not found in the scene. Cannot move object.");
                return;
            }

            // Update the position and rotation of the object to follow the ray cursor
            if (m_rayInteractorCursor != null)
            {
                var targetTransform = m_rayInteractorCursor.transform;
                var targetPosition = targetTransform.position;
                var targetRotation = targetTransform.rotation;

                // Edge case: if the cursor is on the floor or the ceiling, we skip rotation alignment.
                var canUseCursorRotation = targetPosition.y is >= MIN_FLOOR_Y_THRESHOLD and <= MIN_CEILING_Y_THRESHOLD;
                if (canUseCursorRotation)
                {
                    m_rootTransform.rotation = targetRotation;
                }

                if (m_isAttachedToFloor)
                {
                    targetPosition.y = 0.0f;
                }
                m_rootTransform.position = targetPosition;
            }

            // If the trigger is pressed, disable the moving and end the placement of the object.
            if (OVRInput.GetDown(m_stopMoveTrigger))
            {
                m_isMoving = false;

                if (m_goNextGamePhaseAfterPlacement)
                {
                    // Trigger the next game phase
                    GameManagement.GameManager.Instance.NextGameplayPhase();
                }
            }
        }
    }
}
