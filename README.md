This is a package for VRCOSC containing module(s) made by Rycia.
[You can find VRCOSC here. This is a requirement.](https://github.com/VolcanicArts/VRCOSC)



# Modules
These are the modules included in this package.

## Interhaptics
Parameter support for Interhaptics-based devices such as Razer headsets with Razer Sensa haptic feedback.  

Please read this entire section to ensure your setup is correct.  
This requires Razer Synapse to be installed.  

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
Thank you to the Interhaptics and WYVRN team for helping me out with their SDK, since this repo was never an intended-- but a creative usecase!

This can potentially be a replacement for people who use headsets and other devices with Razer's haptic feedback to posess similar functionality of something similar to OSCGoesBRR or a Gigglepuck.  
This includes the following devices:
- Razer Kraken V4 Pro
- Razer Nari Ultimate
- Razer Freyja
- Razer Wolverine V3 Pro  

These are just a few devices, please report to add to the list here if there's a missing device.
