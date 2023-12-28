# Project Configuration
In order for this project to be functional in editor or on device there is some initial setup that needs to be done.

## Application Configuration
To run the project and use the platform services we need to create an application on the [Meta Quest Developer Center](http://developer.oculus.com).

To run on device you will need a Quest application, and to run in editor you will need a Rift application. The following sections will describe the configuration required for the application to run.

### Data Use Checkup
To use the features from the Platform we need to request which kind of data is required for the application. This can be found in the Data Use Checkup section of the application.

<img src="../Documentation/Images/DataUseCheckup.png" width="40%" height="40%" >

And configure the required Data Usage:

<img src="../Documentation/Images/DataUseCheckupOptions.png" width="40%" height="40%" >

- User Id: Oculus Username
- User Profile: Oculus Username

Once completed you will need to submit the request, click the submit button at the bottom. data use checkup submit

To allow sharing of Spatial Anchors the Platform Service Cloud Storage needs to be enabled as well. To enable this go to All Platform Services and then click Add Service Under Cloud Storage

<img src="../Documentation/Images/PlatformServices.png">

Then Enable Automatic Cloud Backup and press submit

<img src="../Documentation/Images/ApplyCloudStorage.png" >

Set the Application ID
We then need to set the application ID in our project in Unity.
The identifier (App ID) can be found in the API section.

<img src="../Documentation/Images/QuestAppSettings.png" >

Then it needs to be placed in the Assets/Resources/OculusPlatformSettings.asset

<img src="../Documentation/Images/PlatformSettings.png"> 
<img src="../Documentation/Images/PlatformSettingsField.png">

## Photon Configuration
**Note, this is only required to play the app in multiplayer mode.**

To get the sample working, you will need to configure Photon with your own account and applications. The Photon base plan is free.

Visit [photonengine.com](https://www.photonengine.com/) and create an account
From your Photon dashboard, click "Create A New App"
We will create a "Fusion" app
First fill out the form making sure to set type to "Photon Fusion". Then click Create.
Your new app will now show on your Photon dashboard. Click the App ID to reveal the full string and copy the value for each app.

Open your unity project and paste your Fusion App ID in [Assets/Photon/Fusion/Resources/PhotonAppSettings](../Assets/Photon/Fusion/Resources/PhotonAppSettings.asset).

<img src="../Documentation/Images/FusionSettings.png">

The Photon Realtime transport should now work. You can check the dashboard in your Photon account to verify there is network traffic.

