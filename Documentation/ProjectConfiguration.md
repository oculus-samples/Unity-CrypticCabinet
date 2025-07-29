# Project Configuration
In order for this project to be functional in editor or on device there is some initial setup that needs to be done.

## Application Configuration
To run the project and use the platform services we need to create an application on the [Meta Horizon Dashboard](https://developers.meta.com/horizon/manage/). Create a new app and select **Meta Horizon Store**. The following sections will describe the configuration required for the application to run.

### Data Use Checkup
To use the features from the Platform we need to request which kind of data is required for the application. This can be found in the Data Use Checkup section of the application.

<img src="../Documentation/Images/DataUseCheckup.png" width="40%" height="40%" >

And configure the required Data Usage:

<img src="../Documentation/Images/DataUseCheckupOptions.png" width="40%" height="40%" >

- User Id: Oculus Username
- User Profile: Oculus Username

Once completed you will need to submit the request, click the submit button at the bottom.

### Set the App ID
We then need to set the application ID in our project in Unity. The identifier (App ID) can be found under **Development** and then **API**.

<img src="../Documentation/Images/QuestAppSettings.png" >

Then it needs to be placed in the Assets/Resources/OculusPlatformSettings.asset

<img src="../Documentation/Images/PlatformSettings.png">
<img src="../Documentation/Images/PlatformSettingsField.png">

### Android Keystore and Package Name
In order to upload your APK to the **Meta Horizon Store** to be able to test with other users, you will need to configure the **Android Keystore** and Package Name. Create a new Keystore in the **Player Settings** and then set a unique package name. This package name will be used to identify your application and cannot be changed in a later version. Lastly, set a unique **Bundle Version Code** and increase it for each new version of your APK.

## Photon Fusion Configuration
> [!NOTE]
> This is only required to play the app in multiplayer mode.

To get the sample working, you will need to configure Photon with your own account and application. The Photon base plan is free.

- Visit [photonengine.com](https://www.photonengine.com/) and create an account.
- From your Photon dashboard, click **Create A New App**.
- Type in a name for the app and make sure to select **Fusion**. Then click **Create**.
- Your new app will now show up on your **Photon dashboard**. Click the **App ID** to reveal the full string and copy the value.
- Open your Unity project and paste your **Fusion App ID** in [Assets/Photon/Fusion/Resources/PhotonAppSettings](../Assets/Photon/Fusion/Resources/PhotonAppSettings.asset) under **Fusion**, then **Realtime Settings**.

<img src="../Documentation/Images/FusionSettings.png">

The Photon Realtime transport should now work. You can check the dashboard in your Photon account to verify there is network traffic.

## Upload to release channel
In order to have colocation working using the [Shared Spatial Anchors](https://developers.meta.com/horizon/documentation/unity/unity-shared-spatial-anchors/), you will first need to upload an initial build to a [release channel](https://developers.meta.com/horizon/resources/publish-release-channels/). For instructions you can go to the [developer center](https://developer.oculus.com/resources/publish-release-channels-upload/). Then to be able to test with other users you will need to add them to that channel (or invite them as members of your organization), more information in the [documentation](https://developer.oculus.com/resources/publish-release-channels-add-users/).

*Once the initial build is uploaded you will be able to use any development build with the same application Id, no need to upload every build to test local changes.*

## Headset permissions
When you first launch the application a permission popup will ask to share **spatial data**, you must confirm if you want to use colocation. If you denied and would like to turn on the feature later, you can go to **Settings > Privacy & Safety > Device Permissions > Enhanced spatial services** and enable it.
