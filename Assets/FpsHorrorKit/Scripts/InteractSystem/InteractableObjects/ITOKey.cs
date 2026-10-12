namespace FpsHorrorKit
{
    using UnityEngine;

    public class ITOKey : MonoBehaviour, IInteractable
    {
        public DoorSystem compatibleDoor;
        [SerializeField] private string interactText = "Take key [E]";
        private bool taken;

        public void Interact()
        {
            if (taken) { return; }
            if (compatibleDoor == null)
            {
                Debug.LogWarning("Key has no compatible door. Assign compatibleDoor in the Inspector.", this);
                return;
            }

            taken = true;
            compatibleDoor.hasKey = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        public void Highlight()
        {
            PlayerInteract.Instance?.ChangeInteractText(interactText);
        }
        public void HoldInteract() { }
        public void UnHighlight() { }
    }
}
