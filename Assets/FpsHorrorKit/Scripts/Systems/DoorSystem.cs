namespace FpsHorrorKit
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Events;


    public class DoorSystem : MonoBehaviour, IInteractable
    {
        [Header("Highlight UI")]
        [SerializeField] private string interactText = "Door Open/Close [E]";
        [SerializeField] private string doorLockedText = "Find Key";
        [SerializeField] private string useKeyText = "Use Key";

        [Header("Door Settings")]
        [Tooltip("Kapı kilitli mi?")] public bool isLocked;
        [Tooltip("Kapının anahtarına sahip mi?")] public bool hasKey;
        [Tooltip("Kapının menteşe etrafında dönme hızı")] public float rotationSpeed = 100f;
        public float endRotation;
        public AudioSource doorAudioSource;

        [Header("Events")]
        public UnityEvent onDoorOpened = new UnityEvent();


        private float startRotation = 0;
        private bool isFinished = false;
        private bool isOpen;

        private void Start()
        {
            isFinished = true;
            startRotation = transform.localEulerAngles.y;
        }

        public void Interact()
        {
            if (hasKey)
            {
                isLocked = false;
            }
            if (isLocked) { return; }

            if (!isOpen && isFinished)
            {
                isOpen = true;
                StartCoroutine(OpenDoor(endRotation, true));
            }
            else if (isOpen && isFinished)
            {
                isOpen = false;
                StartCoroutine(OpenDoor(startRotation, false));
            }

        }

        public void Highlight()
        {
            if (hasKey && isLocked)
            {
                PlayerInteract.Instance.ChangeInteractText(useKeyText);
            }
            else if (isLocked)
            {
                PlayerInteract.Instance.ChangeInteractText(doorLockedText);
            }
            else
            {
                PlayerInteract.Instance.ChangeInteractText(interactText);
            }
        }

        IEnumerator OpenDoor(float targetRotation, bool opening)
        {
            isFinished = false;
            if (doorAudioSource != null) doorAudioSource.Play();

            while (Mathf.Abs(Mathf.DeltaAngle(transform.localEulerAngles.y, targetRotation)) > 0.1f)
            {
                float step = rotationSpeed * Time.deltaTime;
                float newY = Mathf.MoveTowardsAngle(transform.localEulerAngles.y, targetRotation, step);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, newY, transform.localEulerAngles.z);
                yield return null;
            }
            // Son rotasyonu kesinleştir
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, targetRotation, transform.localEulerAngles.z);
            isFinished = true;
            if (opening)
            {
                onDoorOpened?.Invoke();
            }
        }
        public void HoldInteract() { }
        public void UnHighlight() { }
    }
}
