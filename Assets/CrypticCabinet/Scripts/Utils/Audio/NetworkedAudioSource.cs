// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Fusion.Editor;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;

namespace CrypticCabinet.Utils.Audio
{
    /// <summary>
    ///     Replicates loop and play state of AudioSource.
    ///     Note: call Play and Stop on this instead of the AudioSource, otherwise replication will not work!
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(AudioSource))]
    public class NetworkedAudioSource : NetworkBehaviour, IStateAuthorityChanged
    {
        [SerializeField] private AudioSource m_audioSource;

        [Networked(OnChanged = nameof(OnLoopPropertyChanged), OnChangedTargets = OnChangedTargets.Proxies)]
        private bool IsLoopProperty { get; set; }

        [Networked(OnChanged = nameof(OnPlayStateChanged), OnChangedTargets = OnChangedTargets.Proxies)]
        private bool IsPlayingProperty { get; set; }

        private NetworkObject m_networkObject;
        private readonly PendingTasksHandler m_pendingTasksHandler = new();

        private void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            this.AssertField(m_audioSource, nameof(m_audioSource));

            m_networkObject = GetComponent<NetworkObject>();
            if (m_networkObject == null)
            {
                m_networkObject = GetComponentInParent<NetworkObject>();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            if (IsLoopProperty != m_audioSource.loop)
            {
                IsLoopProperty = m_audioSource.loop;
            }
            if (IsPlayingProperty != m_audioSource.isPlaying)
            {
                IsPlayingProperty = m_audioSource.isPlaying;
            }
        }

        public void Play()
        {
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                if (m_audioSource != null)
                {
                    m_audioSource.Play();
                }
            }, HasStateAuthority);
        }

        public void Stop()
        {
            m_pendingTasksHandler.TryExecuteAction(() =>
            {
                if (m_audioSource != null)
                {
                    m_audioSource.Stop();
                }
            }, HasStateAuthority);
        }

        public static void OnPlayStateChanged(Changed<NetworkedAudioSource> changed)
        {
            changed.Behaviour.UpdatePlayState(changed.Behaviour.IsPlayingProperty);
        }

        public static void OnLoopPropertyChanged(Changed<NetworkedAudioSource> changed)
        {
            changed.Behaviour.UpdateLoop(changed.Behaviour.IsLoopProperty);
        }

        private void UpdatePlayState(bool isPlaying)
        {
            if (isPlaying)
            {
                m_audioSource.Play();
            }
            else
            {
                m_audioSource.Stop();
            }
        }

        private void UpdateLoop(bool isLoop)
        {
            m_audioSource.loop = isLoop;
        }

        public void StateAuthorityChanged()
        {
            m_pendingTasksHandler.ExecuteAllOrClear(HasStateAuthority);
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(NetworkedAudioSource))]
    public class NetworkedAudioSourceInspector : NetworkBehaviourEditor
    {
        private const string INFO = "Note: call Play() and Stop() on this component instead of the AudioSource, otherwise network replication will not work!";
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(INFO, MessageType.Info);
            EditorGUILayout.Space(10);
        }
    }

#endif
}
