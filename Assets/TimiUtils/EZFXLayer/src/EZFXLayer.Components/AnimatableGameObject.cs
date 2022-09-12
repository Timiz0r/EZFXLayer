namespace EZFXLayer
{
    using System;
    using UnityEngine;

    [Serializable]
    public class AnimatableGameObject
    {
        public string key = Guid.NewGuid().ToString(); //shared across reference animation and other animations
        //serializes just fine
#pragma warning disable CA2235
        public GameObject gameObject;
#pragma warning restore CA2235
        //the path will only be used for finding a new GameObject if the original is deleted or something
        //TODO: will also need a way to keep it up-to-date if things get renamed
        //  perhaps [UnityEditor.InitializeOnLoad] and EditorSceneManager.activeSceneChangedInEditMode?
        //TODO: prob should make it private, still serialized, and accessible thru more controlled means
        public string path;
        public bool active;

        public bool Matches(AnimatableGameObject gameObject) => gameObject == null
            ? throw new ArgumentNullException(nameof(gameObject))
            : this.key == gameObject.key;

        public AnimatableGameObject Clone() => new AnimatableGameObject()
        {
            key = key,
            gameObject = gameObject,
            path = path,
            active = active
        };
    }
}
