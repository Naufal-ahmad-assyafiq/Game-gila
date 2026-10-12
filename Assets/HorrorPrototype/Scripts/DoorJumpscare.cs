namespace FpsHorrorKit
{
    using UnityEngine;

    /// <summary>Listens to completed door opening; consumed only once per play session.</summary>
    public sealed class DoorJumpscare : MonoBehaviour
    {
        public DoorSystem door;
        public GhostAppearance ghost;
        [Tooltip("Optional. Uses the current MainCamera when left empty.")]
        public Camera viewCamera;
        public bool HasPlayed { get; private set; }

        private DoorSystem subscribedDoor;

        private void OnEnable()
        {
            if (door == null || ghost == null)
            {
                Debug.LogWarning("DoorJumpscare needs both a door and a ghost.", this);
                return;
            }
            subscribedDoor = door;
            subscribedDoor.onDoorOpened.AddListener(PlayOnce);
        }

        private void OnDisable()
        {
            if (subscribedDoor != null)
            {
                subscribedDoor.onDoorOpened.RemoveListener(PlayOnce);
                subscribedDoor = null;
            }
        }

        private void PlayOnce()
        {
            if (HasPlayed) { return; }
            Camera camera = viewCamera != null ? viewCamera : Camera.main;
            if (ghost == null || !ghost.TryAppear(camera)) { return; }
            HasPlayed = true;
        }
    }
}
