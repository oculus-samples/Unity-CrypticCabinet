// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using CrypticCabinet.UI;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     Check if the scene setup is valid, and triggers the relative events to notify its state.
    /// </summary>
    public sealed class SceneSetupValidation : MonoBehaviour
    {
        [SerializeField] private OVRSceneManager m_sceneManagerPrefab;
        private OVRSceneManager m_sceneManager;

        public UnityEvent OnIsValid;
        public UnityEvent OnIsInvalid;

#if !UNITY_EDITOR
        private bool m_sceneCaptureRequested;
#endif

        public void StartValidateSceneSetUp()
        {
            if (m_sceneManager == null)
            {
                m_sceneManager = Instantiate(m_sceneManagerPrefab);
#if !UNITY_EDITOR
                m_sceneCaptureRequested = false;
#endif
                m_sceneManager.SceneModelLoadedSuccessfully += SceneModelLoadedSuccessfully;
                m_sceneManager.NewSceneModelAvailable += NewSceneModelAvailable;
                m_sceneManager.NoSceneModelToLoad += NoSceneModelToLoad;
                m_sceneManager.SceneCaptureReturnedWithoutError += SceneCaptureReturnedWithoutError;
                m_sceneManager.UnexpectedErrorWithSceneCapture += UnexpectedErrorWithSceneCapture;
            }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || (UNITY_ANDROID && !UNITY_EDITOR)
            StartCoroutine(nameof(AttemptToLoadSceneModel));
#endif
        }

        private IEnumerator AttemptToLoadSceneModel()
        {
            UISystem.Instance.HideNetworkSelectionMenu();
            UISystem.Instance.ShowMessage("Attempting to load your room, please wait...");
            do
            {
                yield return null;
            } while (!m_sceneManager.LoadSceneModel());
        }

        private void UnexpectedErrorWithSceneCapture()
        {
            OnIsInvalid?.Invoke();
            CleanUp();
        }

        private void SceneCaptureReturnedWithoutError()
        {
            OnIsInvalid?.Invoke();
            CleanUp();
        }

        private void NoSceneModelToLoad()
        {
            OnNoSceneModelToLoad();
        }

        private void NewSceneModelAvailable()
        {
            OnIsInvalid?.Invoke();
            CleanUp();
        }

        private void SceneModelLoadedSuccessfully()
        {
            OnIsValid?.Invoke();
            CleanUp();
        }

        private void CleanUp()
        {
            var anchors = FindObjectsOfType<OVRSceneAnchor>();
            foreach (var anchor in anchors)
            {
                Destroy(anchor.gameObject);
            }

            m_sceneManager.SceneModelLoadedSuccessfully -= SceneModelLoadedSuccessfully;
            m_sceneManager.NewSceneModelAvailable -= NewSceneModelAvailable;
            m_sceneManager.NoSceneModelToLoad -= NoSceneModelToLoad;
            m_sceneManager.SceneCaptureReturnedWithoutError -= SceneCaptureReturnedWithoutError;
            m_sceneManager.UnexpectedErrorWithSceneCapture -= UnexpectedErrorWithSceneCapture;

            Destroy(m_sceneManager.gameObject);
        }

        private void OnNoSceneModelToLoad()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("Scene Capture does not work over Link",
                "There is no scene model available, and scene capture cannot be invoked over Link. " +
                "Please capture a scene with the HMD in standalone mode, then access the scene model over Link. " +
                "\n\n" +
                "If a scene model has already been captured, make sure the HMD is connected via Link and that is is donned.",
                "Ok");
#else
            if (!m_sceneCaptureRequested)
            {
                m_sceneCaptureRequested = m_sceneManager.RequestSceneCapture();
            }
#endif
        }
    }
}