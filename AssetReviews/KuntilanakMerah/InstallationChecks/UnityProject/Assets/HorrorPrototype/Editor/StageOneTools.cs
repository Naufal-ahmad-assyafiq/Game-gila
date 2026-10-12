namespace FpsHorrorKit.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public static class StageOneTools
    {
        [MenuItem("Tools/Horror Prototype/Validate Stage One")]
        public static void ValidateScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Validate Stage One before entering Play Mode.");
                return;
            }
            GameObject stage = GameObject.Find("StageOne_LockedDoor");
            Require(stage != null, "Open MainScene first.");
            DoorJumpscare trigger = stage.GetComponent<DoorJumpscare>();
            ITOKey key = stage.GetComponentInChildren<ITOKey>();
            Require(trigger != null && trigger.door != null && trigger.ghost != null,
                "DoorJumpscare needs its door and ghost references.");
            Require(trigger.door.transform.parent != null && trigger.door.transform.parent.name == "Single_Door_1",
                "Jumpscare must target Single_Door_1/Door_Leaf.");
            Require(trigger.door.isLocked && !trigger.door.hasKey, "Door must start locked without a key.");
            Require(key != null && key.compatibleDoor == trigger.door, "Key is not linked to Single_Door_1.");
            Require(key.GetComponent<Collider>() != null, "Key needs a collider on the same object as ITOKey.");
            Require(key.GetComponentsInChildren<Collider>().Length == 1, "Key visuals must not block its pickup collider.");
            Require(trigger.ghost.ghostModel != null && !trigger.ghost.ghostModel.activeSelf,
                "Ghost model must start hidden.");
            Require(trigger.ghost.GetComponentsInChildren<Collider>(true).Length == 0,
                "Ghost must not block movement or interaction.");
            Require(trigger.ghost.audioSource != null && trigger.ghost.scareClip != null,
                "Ghost audio source or clip is missing.");
            Debug.Log("Stage One valid: locked door, linked key, hidden ghost and replaceable audio.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) { throw new InvalidOperationException(message); }
        }
    }
}