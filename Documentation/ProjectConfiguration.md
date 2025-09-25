# Project Configuration

To make this project functional in the editor or on a device, some initial setup is required.

## Application Configuration

To run the project and use platform services, create an application on the [Meta Horizon Dashboard](https://developers.meta.com/horizon/manage/). Select **Meta Horizon Store** for your new app. The following sections describe the necessary configuration for the application to run.

### Data Use Checkup

Request the required data for the application in the Data Use Checkup section.

<img src="../Documentation/Images/DataUseCheckup.png" width="40%" height="40%" >

Configure the required Data Usage:

<img src="../Documentation/Images/DataUseCheckupOptions.png" width="40%" height="40%" >

- User Id: Oculus Username
- User Profile: Oculus Username

After completing the configuration, submit the request by clicking the submit button at the bottom.

### Set the App ID

Set the application ID in your Unity project. Find the App ID under **Development** and then **API**.

<img src="../Documentation/Images/QuestAppSettings.png" >

Place the App ID in the Assets/Resources/OculusPlatformSettings.asset.

<img src="../Documentation/Images/PlatformSettings.png">
<img src="../Documentation/Images/PlatformSettingsField.png">

### Android Keystore and Package Name

To upload your APK to the **Meta Horizon Store** for testing with other users, configure the **Android Keystore** and Package Name. Create a new Keystore in **Player Settings** and set a unique package name. This package name identifies your application and cannot be changed later. Set a unique **Bundle Version Code** and increase it for each new APK version.

## Photon Fusion Configuration

> [!NOTE]
> This is only required for multiplayer mode.

To get the sample working, configure Photon with your account and application. The Photon base plan is free.

- Visit [photonengine.com](https://www.photonengine.com/) and create an account.
- In your Photon dashboard, click **Create A New App**.
- Enter a name for the app and select **Fusion**. Then click **Create**.
- Your new app will appear on your **Photon dashboard**. Click the **App ID** to reveal and copy the full string.
- Open your Unity project and paste your **Fusion App ID** in [Assets/Photon/Fusion/Resources/PhotonAppSettings](../Assets/Photon/Fusion/Resources/PhotonAppSettings.asset) under **Fusion**, then **Realtime Settings**.

<img src="../Documentation/Images/FusionSettings.png">

The Photon Realtime transport should now work. Verify network traffic on your Photon account dashboard.

## Upload to Release Channel

To enable colocation using [Shared Spatial Anchors](https://developers.meta.com/horizon/documentation/unity/unity-shared-spatial-anchors/), upload an initial build to a [release channel](https://developers.meta.com/horizon/resources/publish-release-channels/). For instructions, visit the [developer center](https://developers.meta.com/horizon/resources/publish-release-channels-upload/). To test with other users, add them to the channel or invite them as organization members. More information is available in the [documentation](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/).

*Once the initial build is uploaded, you can use any development build with the same application ID without uploading every build to test local changes.*

## Headset Permissions

When you first launch the application, a permission popup will ask to share **spatial data**. Confirm to use colocation. If you initially denied and want to enable the feature later, go to **Settings > Privacy & Safety > Device Permissions > Enhanced spatial services** and enable it.
