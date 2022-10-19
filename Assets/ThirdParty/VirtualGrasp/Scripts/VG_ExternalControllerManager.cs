// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using System.Collections.Generic;
using VirtualGrasp;
using UnityEngine;

/** 
 * VG_ExternalControllerManager exemplifies how you could provide custom controller scripts for your application.
 * The class, used in MyVirtualGrasp.cs, provides a tutorial on the VG API functions for external sensor control.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vgexternalcontrollermanager." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_ExternalControllerManager
{
    /// Map from a wrist (instance ID) to an external controller instance controlling it.
    static private Dictionary<int, List<VG_ExternalController>> m_controllers = new Dictionary<int, List<VG_ExternalController>>();

    static public void Initialize(VG_MainScript vg)
    {
        /// We also assign some haptic signals here.
        VG_Controller.OnObjectCollided.AddListener(TriggerHaptics);
        VG_Controller.OnObjectReleased.AddListener(TriggerHaptics);
        VG_Controller.OnObjectGrasped.AddListener(TriggerHaptics);

        // Clear out the map of wrists and the connected external controllers.
        m_controllers.Clear();
        foreach (VG_SensorSetup sensor in vg.m_sensors)
        {
            if (sensor.m_profile == null || sensor.m_profile.m_sensor != VG_SensorType.EXTERNAL_CONTROLLER) 
                continue;
            foreach (VG_Avatar avatar in sensor.m_avatars)
                RegisterExternalController(avatar.m_avatarID, sensor.m_profile.m_externalType);
        }
    }

    // Register an external controller type for an avatar.
    static public void RegisterExternalController(int avatarID, string controllerType)
    {
        string[] controllerTypes = controllerType.Split(';');
        foreach (VG_HandSide side in new List<VG_HandSide>() { VG_HandSide.LEFT, VG_HandSide.RIGHT })
        {
            if (VG_Controller.GetBone(avatarID, side, VG_BoneType.WRIST, out int wristID, out _) == null)
                continue;
            
            foreach (string controller in controllerTypes)
            {
                if (!m_controllers.ContainsKey(wristID)) m_controllers[wristID] = new List<VG_ExternalController>();
                switch (controller)
                {
                    case "OculusHand": m_controllers[wristID].Add(new VG_EC_OculusHand(avatarID, side)); break;
                    case "UnityXR": m_controllers[wristID].Add(new VG_EC_UnityXRHand(avatarID, side)); break;
                    case "MouseHand": m_controllers[wristID].Add(new VG_EC_MouseHand(avatarID, side)); break;
                    case "LeapHand": m_controllers[wristID].Add(new VG_EC_LeapHand(avatarID, side)); break;
                    case "SteamHand": m_controllers[wristID].Add(new VG_EC_SteamHand(avatarID, side)); break;
                    case "UnityInteractionHand": m_controllers[wristID].Add(new VG_EC_UnityInteractionHand(avatarID, side)); break;
                    default:
                        //VG_Debug.LogWarning("No VG_ExternalController found for \"" + controller + "\". Program it and/or add it to this list. Replacing with VG_EC_GenericHand.");
                        m_controllers[wristID].Add(new VG_EC_GenericHand(avatarID, side));
                        break;
                }
            }
        }

        VG_Controller.RegisterExternalControllers(m_controllers);
    }

    // Process haptics signals (see Listeners in Initialize()).
    static public void TriggerHaptics(VG_HandStatus hand)
    {
        if (m_controllers.TryGetValue(hand.m_hand.GetInstanceID(), out List<VG_ExternalController> wristControllers))
        {
            foreach (VG_ExternalController controller in wristControllers)
                if (controller.m_isTracking) { controller.HapticPulse(hand); return; }
        }
    }
}