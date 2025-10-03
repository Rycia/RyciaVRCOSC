This is a package for VRCOSC containing module(s) made by Rycia.
[You can find VRCOSC here. This is a requirement.](https://github.com/VolcanicArts/VRCOSC)



# Modules
These are the modules included in this package.

## Interhaptics
### As of 10/3/2025, Interhaptics has been moved to its own standalone application at https://github.com/Rycia/InterhapticsVRC due to VRCOSC not being suited for this usecase; this module won't work because it was never complete.

Parameter support for Interhaptics-based devices such as Razer headsets with Razer Sensa haptic feedback.  

This can potentially be a replacement for people who use headsets and other devices with Razer's haptic feedback to posess similar functionality of something similar to OSCGoesBRR or a Gigglepuck.  
Think about it! Instead of buying two Giggle Pucks, or if you already have one of the popular Razer haptic headsets, or another device, you could have haptic headphones instead of shelling out for extra haptic devices that are going to be extra weight strapped to your head during a long VR session, using technology it already supports; it just has to be bridged over to OSC!

This supports the following devices, these are just a few:
- Razer Kraken V4 Pro
- Razer Kraken V3 HyperSense
- Razer Kraken V3 Pro
- Razer Nari Ultimate

These are just a few devices, please report to add to the list here if there's a missing device.  
This list is loose, if you have a Razer headset with haptics, period, it should work. If you don't have a Razer headset but it's haptic, like Steelseries, you can still try it and it may surprisingly work. Many haptic devices by very large companies use WYVRN and Interhaptics as their underlying tech for this very special feedback, so any device that was built with Interhaptics in mind should work.


### Please read this entire section to ensure your setup is correct.  
To start, this requires Razer Synapse 4+ and Razer Chroma to be installed. If you do not have Razer installed, install it, and reboot your computer.

In order to use this module, you must allow the application in your Razer Chroma Studio under Chroma Apps, and make sure Chroma Apps are enabled altogether.
<img width="1282" height="332" alt="image" src="https://github.com/user-attachments/assets/b94ff12b-d0e7-491b-bbfb-f39fc1c3b5d8" />

You must also go to "C:\Users\YOURUSERNAMEHERE\AppData\Roaming\VRCOSC\packages\" and make a dependencies folder,  
[ and put the .dll files in there from the "RyciaVRCOSC/InterhapticsModule/Interhaptics/x64" folder in this repository](https://github.com/Rycia/RyciaVRCOSC/tree/bbfa93afb60947bf4dff0e267d0e309264a50f71/RyciaVRCOSC/InterhapticsModule/Interhaptics/x64).  
This folder will not exist yet, and it causes problems when being put in the  
"C:\Users\YOURUSERNAMEHERE\AppData\Roaming\VRCOSC\packages\local"  
or  
"C:\Users\YOURUSERNAMEHERE\AppData\Roaming\VRCOSC\packages\remote\rycia.vrcosc.modules" folders.
<img width="665" height="202" alt="image" src="https://github.com/user-attachments/assets/ddab85c8-756c-4897-ba06-c6113baf21d8" />  

Doing so anyways will cause issues; such as none of my modules loading at all, so it is hardcoded to use these DLL's from this location and is not included with the normal build.  
These DLL's are essential for hardware integration with Interhaptics, and I don't expect them to update once you set them up.
WYVRN is already included with Razer's installation, so nothing is necessary with WYVRN.

DLL's and heavily modified de-Unity'ed SDK content is used from here from [WYVRN](https://doc.wyvrn.com/docs/wyvrn-sdk/unity/) and [Interhaptics](https://doc.wyvrn.com/docs/interhaptics-sdk/haptics-sdk-for-game-engines/key-concepts/).  
Thank you to the Interhaptics and WYVRN team for personally helping me out with their SDK, since this repo was never an intended-- but a creative usecase!

There is a prefab set up for you to put on your VRChat avatars. [You can get it here](https://github.com/Rycia/RyciaVRCOSC/tree/90c7d83ed2fec07f5618fc01a627d9662fa40b93/RyciaVRCOSC/prefabs) or at "RyciaVRCOSC/prefabs/InterhapticsOSC". An INSTRUCTIONS.txt is available with the download. You can put it anywhere on your avatar, and constrain it to the head. They're just a few contacts and parameters pre-set up with a global toggle for your avatar.
<img width="1320" height="896" alt="{7713FA8D-FF35-45DA-A846-9D09E93DC051}" src="https://github.com/user-attachments/assets/ddbf8960-651a-4aa7-be0b-94028cc81547" />

