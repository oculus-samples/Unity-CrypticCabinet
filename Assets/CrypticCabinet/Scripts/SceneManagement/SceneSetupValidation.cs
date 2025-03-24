// Copyright (c) Meta Platforms, Inc. and affiliates.

using CrypticCabinet.UI;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     Check if the scene setup is valid, and triggers the relative events to notify its state.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public sealed class SceneSetupValidation : MonoBehaviour
    {
        public UnityEvent OnIsValid;
        public UnityEvent OnIsInvalid;

#if !UNITY_EDITOR
        private bool m_sceneCaptureRequested;
#endif

        public void StartValidateSceneSetUp()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || (UNITY_ANDROID && !UNITY_EDITOR)
            UISystem.Instance.HideNetworkSelectionMenu();
            UISystem.Instance.ShowMessage("Attempting to load your room, please wait...");

            MRUK.Instance.LoadSceneFromDevice().ContinueWith(task =>
            {
                switch (task.Result)
                {
                    case MRUK.LoadDeviceResult.Success:
                        SceneModelLoadedSuccessfully();
                        break;
                    case MRUK.LoadDeviceResult.NoScenePermission:
                        break;
                    case MRUK.LoadDeviceResult.NoRoomsFound:
                        NoSceneModelToLoad();
                        break;
                    default:
                        Debug.LogError("Load local scene failed with Result: " + task.Result);
                        break;
                }
            });
#endif
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
#endif
        }
    }
}
