// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Threading.Tasks;
using Meta.XR.Samples;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.Events;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Utility class to configure the Oculus platform.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public class OculusPlatformUtils : MonoBehaviour
    {
        public static UnityEvent OnOculusPlatformSDKInitialized = new();
        public static UnityEvent OnOculusCoreInitializationFailed = new();
        public static UnityEvent OnEntitlementCheckFailed = new();
        public static UnityEvent<User> OnOculusUserLoginSuccess = new();

        /// <summary>
        ///     Represents the Oculus user that is logged in and using this app.
        /// </summary>
        private static User s_loggedInUser;

        private static ulong? s_userDeviceGeneratedUid;

        /// <summary>
        ///     Initializes the Oculus Core, checks entitlements, retrieves logged user.
        /// </summary>
        /// <returns>True if no error occurs during initialization.</returns>
        public static async Task<bool> InitializeOculusPlatformSDK()
        {
            // Initialize the Core, if not initialized yet.
            if (!await InitOculusCore())
            {
                return false;
            }

            // Check Entitlement
            var isUserEntitled = await Entitlements.IsUserEntitledToApplication().Gen();
            if (isUserEntitled.IsError)
            {
                var error = isUserEntitled.GetError();
                Debug.LogError($"[OculusPlatformUtils] Entitlement failed: {error.Message}({error.Code})");
                OnEntitlementCheckFailed?.Invoke();
                return false;
            }

            Debug.Log("Oculus Platform SDK entitlement check success");

            // Get info about the logged in user from the Oculus platform, if not yet retrieved.
            if (s_loggedInUser == null)
            {
                return await LoadLoggedInUser();
            }

            // Initialization successfully completed, and logged in user was already retrieved.
            OnOculusPlatformSDKInitialized?.Invoke();
            return true;
        }

        /// <summary>
        ///     Returns the logged in Oculus user.
        /// </summary>
        /// <returns>The Oculus user currently using the app and logged in.</returns>
        public static async Task<User> GetLoggedInUser()
        {
            if (s_loggedInUser == null)
            {
                _ = await LoadLoggedInUser();
            }

            return s_loggedInUser;
        }

        /// <summary>
        /// Get the generated unique id of the user device. This will change on every session.
        /// </summary>
        /// <returns>generated unique id as a ulong</returns>
        public static ulong GetUserDeviceGeneratedUid()
        {
            s_userDeviceGeneratedUid ??= (ulong)Guid.NewGuid().GetHashCode();

            return s_userDeviceGeneratedUid.Value;
        }

        /// <summary>
        ///     Ensures the Oculus Core has been initialized.
        ///     If it was already initialized, it returns true.
        /// </summary>
        /// <returns>True if the Oculus Core has been initialized successfully.</returns>
        private static async Task<bool> InitOculusCore()
        {
            // Initialize the Core, if not initialized yet.
            if (!Core.IsInitialized())
            {
                Debug.Log("Initializing Oculus Platform SDK");
                var coreInitResult = await Core.AsyncInitialize().Gen();
                if (coreInitResult.IsError)
                {
                    var error = coreInitResult.GetError();
                    Debug.LogError(
                        $"[OculusPlatformUtils] Failed Oculus Platform SDK Initialization: {error.Message}({error.Code})");
                    OnOculusCoreInitializationFailed?.Invoke();
                    return false;
                }

                Debug.Log("Oculus Platform SDK initialized successfully");
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="onError"></param>
        /// <returns></returns>
        private static async Task<bool> LoadLoggedInUser(Action<string> onError = null)
        {
            // Ensure Oculus Core was initialized before proceeding.
            if (!await InitOculusCore())
            {
                Debug.LogError("[OculusPlatformUtils] LoadLoggedInUser called, but Oculus Core not ready!");
                return false;
            }

#if UNITY_EDITOR
            Debug.Log("Editor user is not actually logged in. Forcing success.");
            OnOculusUserLoginSuccess.Invoke(null);
            return true;
#else
            var loggedInUserResult = await Users.GetLoggedInUser().Gen();
            if (!loggedInUserResult.IsError && loggedInUserResult.Type == Message.MessageType.User_GetLoggedInUser)
            {
                s_loggedInUser = loggedInUserResult.GetUser();
                Debug.Log(
                    $"OculusPlatform - Logged in as {s_loggedInUser.DisplayName} ({s_loggedInUser.OculusID}, {s_loggedInUser.ID})");
                OnOculusUserLoginSuccess?.Invoke(s_loggedInUser);
                return true;
            }

            var error = loggedInUserResult.GetError();
            Debug.LogError($"[OculusPlatformUtils] Failed to retrieve logged in user: {error.Message}({error.Code})");
            return false;
#endif
        }
    }
}