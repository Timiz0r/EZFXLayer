namespace EZUtils.EZFXLayer
{
    using System;
    using UnityEngine;

    [Serializable]
    public class AnimatableGameObject
    {
        public string key = Guid.NewGuid().ToString(); //shared across reference animation and other animations

        public GameObject gameObject;
        //the path will only be used for finding a new GameObject if the original is deleted or something
        //TODO: will also need a way to keep it up-to-date if things get renamed
        //  perhaps [UnityEditor.InitializeOnLoad] and EditorSceneManager.activeSceneChangedInEditMode?
        //TODO: prob should make it private, still serialized, and accessible thru more controlled means
        public string path;
        public bool active;

        //in the event these are for the reference itself, wont be used
        public bool synchronizeActiveWithReference;
        public bool disabled;

        private AnimatableGameObject() { }

        public AnimatableGameObject(
            GameObject gameObject, string path, bool active, bool synchronizeActiveWithReference, bool disabled)
        {
            this.gameObject = gameObject;
            this.path = path;
            this.active = active;
            this.synchronizeActiveWithReference = synchronizeActiveWithReference;
            this.disabled = disabled;
        }

        public bool Matches(AnimatableGameObject gameObject) => gameObject == null
            ? throw new ArgumentNullException(nameof(gameObject))
            : key == gameObject.key;

        public AnimatableGameObject Clone()
            => new AnimatableGameObject(
                gameObject: gameObject,
                path: path,
                active: active,
                synchronizeActiveWithReference: synchronizeActiveWithReference,
                disabled: disabled)
            {
                key = key
            };
    }
}
