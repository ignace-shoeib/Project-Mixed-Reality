// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using UnityEngine.UI; // for Text UI
using System.Collections.Generic;
using VirtualGrasp;

/**
 * VG_Highlighter exemplifies how you could enable runtime grasp editing into your application.
 * The MonoBehavior provides a tutorial on the VG API functions for some of the VG_Controller event functions, 
 * such as EditGrasp, GetInteractionTypeForObject and SetInteractionTypeForObject.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vggraspannotator." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_GraspEditor : MonoBehaviour
{
    public Transform m_pad = null;
    public Transform m_addGraspButton = null;
    public Transform m_toggleInteractionButton = null;
    public Transform m_stepGraspButton = null;
    public Transform m_deleteGraspButton = null;
    public Transform m_deleteAllGraspsButton = null;

    private List<ButtonContainer> m_containers = new List<ButtonContainer>();
    
    // Keep track of intersections
    private Dictionary<Transform, bool> m_intersections = new Dictionary<Transform, bool>();

    private delegate void EditFunction(VG_HandStatus hand);
    private delegate bool ValidateFunction(VG_HandStatus hand, out string text);
    private class ButtonContainer
    {
        public Transform m_root = null;
        public EditFunction m_editFunction = null;
        public ValidateFunction m_validateFunction = null;
        private Text m_text = null;
        private MeshRenderer m_renderer = null;
        private VG_Articulation m_articulation = null;
        public static HashSet<Transform> BUTTON_TRANSFORMS = new HashSet<Transform>();
        public ButtonContainer(Transform button, ValidateFunction validateFunc, EditFunction editFunc)
        {
            m_root = button;
            m_text = button.GetComponentInChildren<Text>();
            m_renderer = button.GetComponentInChildren<MeshRenderer>();
            m_articulation = button.GetComponentInChildren<VG_Articulation>();
            BUTTON_TRANSFORMS.Add(m_articulation.transform);
            m_editFunction = editFunc;
            m_validateFunction = validateFunc;
        }

        public bool Validate(VG_HandStatus hand)
        {
            bool valid = m_validateFunction(hand, out string text);
            if (m_text != null)
            {
                m_text.text = text;
                m_text.color = (valid && hand.IsHolding()) ? Color.black : Color.grey;
            }
            return valid;
        }

        public bool Trigger(VG_HandStatus hand, bool hasIntersection)
        {
            if (m_renderer == null || !hand.IsHolding())
                return hasIntersection;

            VG_Controller.GetObjectJointState(m_root, out float state);
            float threshold = m_articulation.m_min + (m_articulation.m_max - m_articulation.m_min) * 0.2f;

            bool isIntersecting = state > threshold;
            if (hasIntersection != isIntersecting)
            {
                VG_Controller.OnObjectCollided.Invoke(hand); // just to trigger haptics
                if (isIntersecting) m_editFunction(hand);
            }

            return isIntersecting;
        }
    }

    private void Start()
    {
        m_containers.Add(new ButtonContainer(m_addGraspButton, ValidateAddGrasp, AddGrasp));
        m_containers.Add(new ButtonContainer(m_toggleInteractionButton, ValidateToggleInteraction, ToggleInteraction));
        m_containers.Add(new ButtonContainer(m_stepGraspButton, ValidateStepGrasp, StepGrasp));
        m_containers.Add(new ButtonContainer(m_deleteGraspButton, ValidateDeleteGrasp, DeleteGrasp));
        m_containers.Add(new ButtonContainer(m_deleteAllGraspsButton, ValidateDeleteAllGrasp, DeleteAllGrasp));
    }

    #region ContainerFunctions

    private bool IsValidObject(VG_HandStatus hand)
    {
        return hand != null && hand.m_selectedObject != null && hand.m_selectedObject.TryGetComponent<MeshRenderer>(out _);
    }

    private void AddGrasp(VG_HandStatus hand)
    {
        VG_Controller.EditGrasp(hand.m_avatarID, hand.m_side, VG_EditorAction.ADD_CURRENT);
    }

    private bool ValidateAddGrasp(VG_HandStatus hand, out string text)
    {
        if (!IsValidObject(hand))
        {
            text = "Add Grasp";
            return false;
        }

        if (VG_Controller.GetInteractionTypeForObject(hand.m_selectedObject) == VG_InteractionType.JUMP_PRIMARY_GRASP)
        {
            text = "No add grasp\ninteraction is JUMP_PRIMARY_GRASP!";
            return false;
        }

        text = "Add Grasp\n(" + hand.GetNumGrasps() + ")";
        return true;
    }
    private void ToggleInteraction(VG_HandStatus hand)
    {
        VG_Controller.SetInteractionTypeForObject(hand.m_selectedObject,
                VG_Controller.GetInteractionTypeForObject(hand.m_selectedObject) != VG_InteractionType.JUMP_PRIMARY_GRASP ?
                VG_InteractionType.JUMP_PRIMARY_GRASP : VG_InteractionType.TRIGGER_GRASP);
    }

    private void StepGrasp(VG_HandStatus hand)
    {
        VG_Controller.TogglePrimaryGraspOnObject(hand.m_avatarID, hand.m_side, hand.m_selectedObject);
    }

    private bool ValidateToggleInteraction(VG_HandStatus hand, out string text)
    {
        if (!IsValidObject(hand))
        {
            text = "Toggle interaction";
            return false;
        }

        if (hand.GetNumGrasps() == 0)
        {
            text = "No toggle interaction\nno grasp!";
            return false;
        }

        VG_InteractionType currentInteractionType = VG_Controller.GetInteractionTypeForObject(hand.m_selectedObject);
        VG_InteractionType destInteractionType = (currentInteractionType == VG_InteractionType.JUMP_PRIMARY_GRASP) ? VG_InteractionType.TRIGGER_GRASP : VG_InteractionType.JUMP_PRIMARY_GRASP;
        text = "Toggle interaction\n" + currentInteractionType + "";
        return true;
    }

    private bool ValidateStepGrasp(VG_HandStatus hand, out string text)
    {
        if (!IsValidObject(hand))
        {
            text = "Step grasp";
            return false;
        }

        if (hand.GetNumGrasps() == 0)
        {
            text = "No step grasp\nno grasp!";
            return false;
        }

        VG_InteractionType currentInteractionType = VG_Controller.GetInteractionTypeForObject(hand.m_selectedObject);

        if (currentInteractionType != VG_InteractionType.JUMP_PRIMARY_GRASP)
        {
            text = "No step grasp\ninteraction is not JUMP_PRIMARY_GRASP!";
            return false;
        }

        text = "Step grasp";
        return true;
    }

    private bool ValidateDeleteGrasp(VG_HandStatus hand, out string text)
    {
        if (!IsValidObject(hand))
        {
            text = "Delete grasp";
            return false;
        }
        int numGrasps = hand.GetNumGrasps();
        if (numGrasps == 0)
        {
            text = "No delete grasp";
            return false;
        }
        text = "Delete grasp\n(" + numGrasps + ")";
        return true;
    }

    private bool ValidateDeleteAllGrasp(VG_HandStatus hand, out string text)
    {
        if (!IsValidObject(hand))
        {
            text = "Delete all grasps";
            return false;
        }
        int numGrasps = hand.GetNumGrasps();
        if (numGrasps == 0)
        {
            text = "No delete all grasps";
            return false;
        }
        text = "Delete all grasps\n(" + numGrasps + ")";
        return true;
    }

    private void DeleteGrasp(VG_HandStatus hand)
    {
        VG_Controller.EditGrasp(hand.m_avatarID, hand.m_side, VG_EditorAction.DELETE_CURRENT);
        if (hand.GetNumGrasps() == 0)
            VG_Controller.SetInteractionTypeForObject(hand.m_selectedObject, VG_InteractionType.TRIGGER_GRASP);
    }

    private void DeleteAllGrasp(VG_HandStatus hand)
    {
        VG_Controller.EditGrasp(hand.m_avatarID, hand.m_side, VG_EditorAction.DELETE_ALL_HAND_GRASPS);
        if (VG_Controller.GetNumGrasps(hand.m_selectedObject, hand.m_avatarID, hand.m_side) == 0)
            VG_Controller.SetInteractionTypeForObject(hand.m_selectedObject, VG_InteractionType.TRIGGER_GRASP);
        else
            VG_Debug.LogError("After remove all hand grasp, num grasps still non zero");
    }

    #endregion // ContainerFunctions

    void Update()
    {
        // Only consider the sensor controlled avatar.
        VG_Controller.GetSensorControlledAvatarID(out int avatarID);

        // Find selected object for left and right hand, but ignore if selected object is the pad or a button for this annotator
        Transform leftSelected = VG_Controller.GetHand(avatarID, VG_HandSide.LEFT).m_selectedObject;
        leftSelected = ButtonContainer.BUTTON_TRANSFORMS.Contains(leftSelected) || leftSelected == m_pad ? null : leftSelected;
        Transform rightSelected = VG_Controller.GetHand(avatarID, VG_HandSide.RIGHT).m_selectedObject;
        rightSelected = ButtonContainer.BUTTON_TRANSFORMS.Contains(rightSelected) || rightSelected == m_pad ? null : rightSelected;

        // If no object selected or if both hand selected different objects, not allow annotating
        if ((leftSelected == null && rightSelected == null) ||
            (leftSelected != null && rightSelected != null && leftSelected != rightSelected))
        {
            foreach (ButtonContainer container in m_containers)
                if (!container.Validate(null)) continue;
            return;
        }

        bool hasIntersection;
        VG_HandStatus hand = VG_Controller.GetHand(avatarID, (leftSelected != null) ? VG_HandSide.LEFT : VG_HandSide.RIGHT);
        foreach (ButtonContainer container in m_containers)
        {
            if (!container.Validate(hand)) continue;
            // cache intersecting value in map so function only triggers in entering/exiting frame 
            hasIntersection = (m_intersections.ContainsKey(container.m_root)) ? m_intersections[container.m_root] : false;
            m_intersections[container.m_root] = container.Trigger(hand, hasIntersection);
        }
    }
}
