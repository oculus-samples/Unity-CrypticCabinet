// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Fusion;
using Fusion.Editor;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;

namespace CrypticCabinet.Utils.Audio
{
    /// <summary>
    ///     Replicates loop and play state of AudioTrigger.
    ///     Note: use this component instead of AudioTrigger to set the audio source, the clips and call Play(), otherwise network replication will not work!
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(AudioTrigger))]
    public class NetworkedAudioTrigger : NetworkBehaviour, IStateAuthorityChanged
    {
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioTrigger m_audioTrigger;
        [SerializeField] private AudioClip[] m_audioClips;

        [Networked(OnChanged = nameof(OnClipChanged), OnChangedTargets = OnChangedTargets.Proxies)]
        private string CurrentClipName { get; set; }

        private readonly Dictionary<string, AudioClip> m_clipsByName = new();

        [Networked(OnChanged = nameof(OnLoopPropertyChanged), OnChangedTargets = OnChangedTargets.Proxies)]
        private bool IsLoopProperty { get; set; }

        [Networked(OnChanged = nameof(OnPlayStateChanged), OnChangedTargets = OnChangedTargets.Proxies)]
        private bool IsPlayingProperty { get; set; }

        private NetworkObject m_networkObject;
        private readonly PendingTasksHandler m_pendingTasksHandler = new();

        private void Awake()
        {
            // Note: we inject the audio source and the clips from this component.
            // This is the best way for us to have all clients aligned.
            m_audioSource = GetComponent<AudioSource>();
            this.AssertField(m_audioSource, nameof(m_audioSource));
            m_audioTrigger = GetComponent<AudioTrigger>();
            this.AssertField(m_audioTrigger, nameof(m_audioTrigger));
            m_audioTrigger.InjectAudioSource(m_audioSource);

            Debug.Assert(m_audioClips is { Length: > 0 });
            if (m_audioClips != null)
            {
                if (m_audioClips.Length > 0)
                {
                    // Store all clips by name, so that we can detect which clip should
                    // be played by all users (since the random clip is not public on AudioTrigger).
                    foreach (var clip in m_audioClips)
                    {
                        m_clipsByName.Add(clip.name, clip);
                    }
                    m_audioTrigger.InjectAudioClips(m_audioClips);
                }
                else
                {
                    Debug.LogError("No clips set on NetworkedAudioTrigger! No sound can be replicated.");
                }
            }
            else
            {
                Debug.LogError("Audio clips not initialized on NetworkAudioTrigger! No sound can be replicated.");
            }

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

            if (IsLoopProperty != m_audioTrigger.Loop)
            {
                IsLoopProperty = m_audioTrigger.Loop;
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
                // Note: PlayAudio sets a random new clip on the audio source.
                // To tell the other users which clip it is, we check after the play
                // the current clip of the audio source, and we propagate the change
                // to all other users.
                m_audioTrigger.PlayAudio();
                CurrentClipName = m_audioSource.clip.name;
            }, HasStateAuthority);
        }

        private void PlayIncomingClip(string clipName)
        {
            CurrentClipName = clipName;

            // Only play if we have a set of valid clips to use
            if (m_clipsByName is not { Count: > 0 })
            {
                return;
            }

            if (m_clipsByName.TryGetValue(CurrentClipName, out var value))
            {
                m_audioSource.clip = value;
                m_audioSource.Play();
            }
            else
            {
                Debug.LogError($"No clip with name {CurrentClipName} was found in audio source! Networked Audio Trigger failed.");
            }
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

        public static void OnClipChanged(Changed<NetworkedAudioTrigger> changed)
        {
            // The only case when the clip changes is because the state authority
            // called Play, so we automatically play the selected new clip on
            // all other players.
            changed.Behaviour.PlayIncomingClip(changed.Behaviour.CurrentClipName);
        }

        public static void OnPlayStateChanged(Changed<NetworkedAudioTrigger> changed)
        {
            changed.Behaviour.UpdatePlayState(changed.Behaviour.IsPlayingProperty);
        }

        public static void OnLoopPropertyChanged(Changed<NetworkedAudioTrigger> changed)
        {
            changed.Behaviour.UpdateLoop(changed.Behaviour.IsLoopProperty);
        }

        public void StateAuthorityChanged()
        {
            m_pendingTasksHandler.ExecuteAllOrClear(HasStateAuthority);
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(NetworkedAudioTrigger))]
    public class NetworkedAudioTriggerInspector : NetworkBehaviourEditor
    {
        private const string INFO = "Note: use this component instead of AudioTrigger to set the audio source, the clips and call Play(), otherwise network replication will not work!";
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
