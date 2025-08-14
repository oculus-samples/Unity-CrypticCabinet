// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrypticCabinet.GameManagement;
using CrypticCabinet.UI;
using CrypticCabinet.Utils;
using Fusion;
using Meta.XR.Samples;
using Oculus.Platform.Models;
using Meta.Utilities;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace CrypticCabinet.Colocation
{
    /// <summary>
    ///     Manages the complete workflow to ensure that all existing and new users will be colocated correctly
    ///     into the room.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class ColocationDriverNetObj : NetworkSingleton<ColocationDriverNetObj>
    {
        /// <summary>
        ///     Callback for when the colocation process completes.
        ///     If succeeded, the callback will be passed a true, otherwise a false.
        /// </summary>
        public Action<bool> OnColocationCompletedCallback;

        [SerializeField] private GameObject m_clientMRUK;

        private const int RETRY_ATTEMPTS_ALLOWED = 3;
        private int m_currentRetryAttempts;
        private bool m_colocationSuccessful;

        private readonly Guid m_groupUuid = Guid.NewGuid();

        public override void Spawned()
        {
            // Initialize colocation regardless on single or multiplayer session.
            UISystem.Instance.ShowMessage("Waiting for colocation to be ready, please wait...", null, -1);

            SetupForColocation();
        }

        private async void SetupForColocation()
        {
            Debug.Log("SetupForColocation: Initializing Colocation for the player");


            if (HasStateAuthority)
            {
                Debug.Log($"[{nameof(ColocationDriverNetObj)}] hosting colocation", this);
                OnColocationCompleted(MRUK.LoadDeviceResult.Success);
            }
            else
            {
                var user = await OculusPlatformUtils.GetLoggedInUser();
                Debug.Log($"[{nameof(ColocationDriverNetObj)}] requesting colocation for user '{user.ID}'", this);
                ShareSceneServerRpc(user.ID);
            }
        }


        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        private async void ShareSceneServerRpc(ulong oculusUserId, RpcInfo info = default)
        {
            foreach (var r in MRUK.Instance.Rooms)
            {
                if (!r.Anchor.TryGetComponent<OVRSharable>(out var sharableComponent)) {
                    Debug.LogError("Anchor does not support sharing.");
                    return;
                }

                if (await sharableComponent.SetEnabledAsync(true)) {
                    Debug.Log("Anchor is now sharable.");
                } else {
                    Debug.LogError("Unable to enable the sharable component.");
                }
            }

            Debug.Log($"Sharing current room with {oculusUserId}", this);
            if (!OVRSpaceUser.TryCreate(oculusUserId, out var spaceUser))
            {
                Debug.LogError($"Failed to create space user for oculus id {oculusUserId}", this);
                return;
            }

            var result = await MRUK.Instance.ShareRoomsAsync(MRUK.Instance.Rooms, m_groupUuid);
            if (!result.Success)
            {
                Debug.LogError($"Failed to share {MRUK.Instance.Rooms.Count} rooms with user '{oculusUserId}', result = {result}", this);
                return;
            }

            var roomGuids = MRUK.Instance.Rooms.Select(room => room.Anchor.Uuid.ToString()).ToArray();
            var floorAnchor = MRUK.Instance.GetCurrentRoom().FloorAnchor;

            Debug.Log($"Sending shared scene guids = {roomGuids.ListToString()}", this);
            ReceiveSharedSceneClientRpc(info.Source, roomGuids, m_groupUuid.ToString(), floorAnchor.transform.position.ToString(), floorAnchor.transform.rotation.ToString());
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private async void ReceiveSharedSceneClientRpc([RpcTarget] PlayerRef player, string[] roomStrings, string groupString, string floorPositionString, string floorRotationString)
        {
            var roomGuids = roomStrings.Select(str => new Guid(str)).ToArray();
            var roomGuidsString = roomGuids.ListToString();
            var groupUuid = new Guid(groupString);

            Debug.Log($"Received shared room guids = {roomGuidsString}; loading...", this);

            Debug.Assert(MRUK.Instance == null, "There is an MRUK Instance already");
            m_clientMRUK.SetActive(true);
            var floorAnchorPose = new PoseSerializable
            {
                position = new Vector4Serializable(floorPositionString),
                rotation = new Vector4Serializable(floorRotationString)
            };
            var result = await MRUK.Instance.LoadSceneFromSharedRooms(roomGuids, groupUuid, (alignmentRoomUuid: roomGuids[0], floorWorldPoseOnHost: floorAnchorPose.ToPose()));
            OnColocationCompleted(result);
        }

        private void OnColocationCompleted(MRUK.LoadDeviceResult result)
        {
            if (result is MRUK.LoadDeviceResult.Success)
            {
                Debug.Log("Colocation is Ready!", this);
                m_colocationSuccessful = true;
                OnColocationCompletedCallback?.Invoke(true);
            }
            else
            {
                Debug.Log($"Colocation failed! {result}", this);
                OnColocationCompletedCallback?.Invoke(false);
            }
        }

        public IEnumerator RetryColocation()
        {
            Debug.Log($"Retrying colocation (retry #{m_currentRetryAttempts})", this);
            yield return new WaitForSeconds(5f);

            if (m_colocationSuccessful)
            {
                yield break;
            }

            if (m_currentRetryAttempts >= RETRY_ATTEMPTS_ALLOWED)
            {
                GameManager.Instance.RestartGameplay();
                yield break;
            }

            m_currentRetryAttempts++;

            SetupForColocation();
        }

        [Serializable]
        private struct Vector4Serializable
        {
            public float x, y, z, w;
            public Vector4Serializable(Vector4 v) { x = v.x; y = v.y; z = v.z; w = v.w; }
            public Vector4Serializable(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
            public Vector4 ToVector4() => new Vector4(x, y, z, w);
            public Vector3 ToVector3() => new Vector3(x, y, z);
            public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);

            public Vector4Serializable(string str)
            {
                var components = str.Trim('(', ')').Split(',');
                x = float.Parse(components[0].Trim());
                y = float.Parse(components[1].Trim());
                z = float.Parse(components[2].Trim());
                w = components.Length == 4 ? float.Parse(components[3].Trim()) : 0;
            }
        }

        [Serializable]
        private struct PoseSerializable
        {
            public Vector4Serializable position;
            public Vector4Serializable rotation;
            public Pose ToPose() => new Pose(position.ToVector3(), rotation.ToQuaternion());
        }
    }

}
