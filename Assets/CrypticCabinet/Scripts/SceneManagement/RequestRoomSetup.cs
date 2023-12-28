// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace CrypticCabinet.SceneManagement
{
    /// <summary>
    ///     This is a convenient script to initiate / re-calibrate the Room Setup when needed.
    /// </summary>
    public class RequestRoomSetup : MonoBehaviour
    {
        [SerializeField] private OVRSceneManager m_sceneManager;

        /// <summary>
        /// Triggers the Room Setup workflow, even if a room setup is already in place.
        /// </summary>
        public void InitiateRoomSetup()
        {
            if (!m_sceneManager)
            {
                Debug.LogError("Scene manager not set, cannot initiate room setup!");
                return;
            }

            _ = m_sceneManager.RequestSceneCapture();
        }
    }
}