namespace EZFXLayer
{
    using System;
    using UnityEngine;

    [Serializable]
    public class AnimatableGameObject
    {
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
            : this.gameObject == gameObject.gameObject;

        public AnimatableGameObject Clone() => new AnimatableGameObject()
        {
            gameObject = gameObject,
            path = path,
            active = active
        };
    }
}
