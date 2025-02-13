vr_hand_gaze_annotation
=======================

Point cloud annotation tool with hand and gaze tracking in virtual reality in Unity.

The software was developed for a Meta Quest Pro headset, connected using Quest Link. Unity version was 2021.3.16f1.

The reference paper is still under review.

Ground truth point clouds used in the paper can be downloaded from [the RIMLab website](https://rimlab.ce.unipr.it/~rmonica/vr2025_clouds_and_ground_truth.zip).

This software also supports annotation using bounding boxes. The code was integrated from [PointAtMe](https://github.com/florianwirth/PointAtMe) and heavily modified.

Installation
------------

**1. Create an empty 3D project**

Create an empty 3D project in Unity.

**2. Install Dependencies**

Dependencies:

- Oculus XR Plugin
- Oculus Integration
- TextMesh Pro
- Universal RP (Universal Rendering Pipeline, URP)
- Unity Movement

All dependencies except Unity Movement can be installed from the Unity repository using the Package Manager.

Unity Movement can be downloaded from [GitHub](https://github.com/oculus-samples/Unity-Movement).
Install Unity Movement by using the "Add package from disk..." option in the Package Manager. Select the `package.json` file in the root of the Unity Movement repository.

**3. Configure Dependencies**

Ensure that the Universal Rendering Pipeline is enabled. Open the Project Settings (`Edit` → `Project Settings`) and select the `Graphics` tab on the left. In the field `Scriptable Rendering Pipeline Settings`, load one of the URP pipelines (e.g. URP-Balanced provided within this repository).

Eye tracking must be enabled from the Quest Link desktop application (a Meta developer account is required). You may also need to switch the target platform to Android in Unity (`File` → `Build Settings` → `Android`).

**4. Copy the Asset folder**

Close the Unity editor. Copy the content of the Asset folder in this repository into the Asset folder of your project.

**5. Build the C++ plugin**

Dependencies:

- [Point Cloud Library](https://pointclouds.org/) (PCL)
- Microsoft Visual Studio
- [CMake](https://cmake.org)

The C++ plugin is a standard CMake project, located in folder `Assets/rviz_cloud_annotation_plugin`.

- Change variable `PCL_DIR` in `CMakeLists.txt` to point to your PCL installation.
- Create a build folder and generate a Visual Studio project using CMake (for example, CMake-gui).
- Open the project with Visual Studio and build the plugin in Release mode.

As long as the resulting `rviz_cloud_annotation_plugin.dll` library is anywhere within the `Asset` folder, Unity should be able to load it. Please note that the library cannot be rebuilt while the Unity editor is open.

**6. Open the scene**

Open the Unity project and add the scene `Assets/Scenes/EyeTracking.unity`. Hopefully, it should load without errors.

Usage
-----

The annotation mode (Controller, Eye or Box) is selected from a drop-down menu on the ModeController script in the Unity editor. The script is attached to the Canvas in the scene hierarchy.

Point clouds are loaded by PCL in PCD format.
The current point cloud is selected from another drop-down menu in the ModeController script. To add more point clouds, add more entries to the SelectedScene enum in `ModeController.cs` and edit the Start function of `PointCloudGenerator.cs`.