#if UNITY_EDITOR
namespace TimiUtils.EZFXLayerOld
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    //TODO: do testing for when certain game objects from the scene are deleted, etc.
    //want to be able to maintain settings even when this happens.
    //for BlendShape, hopefully storing the skinnedmeshrenderer of an asset will work
    //for GameObject, it's hard. perhaps we store the name, allow the user to swap to a new gameobject, and we try to recover if we find matching gameobjects.
    //  or maybe, if we find all matches, we auto-recover?
    [Serializable]
    public class AnimationSet
    {
        public string name;
        //useful, for instance, for gestures, where we typically keep the state names the same but have animations
        //with actually accurate names
        public string animatorStateNameOverride;

        public bool isFoldedOut = true;

        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();

        //overrides the AnimatorLayer's one. tho, typically, one wouldn't do this; just clarifying behavior in case done.
        public string menuPath = null;

        public void ProcessUpdatedDefault(AnimationSet defaultAnimationSet)
        {
            blendShapes.RemoveAll(bs => !defaultAnimationSet.blendShapes.Any(dbs => bs.Matches(dbs)));
            gameObjects.RemoveAll(go => !defaultAnimationSet.gameObjects.Any(dgo => go.Matches(dgo)));

            blendShapes.AddRange(
                defaultAnimationSet.blendShapes
                    .Where(dbs => !blendShapes.Any(bs => bs.Matches(dbs)))
                    .Select(bs => bs.Clone()));
            gameObjects.AddRange(
                defaultAnimationSet.gameObjects
                    .Where(dgo => !gameObjects.Any(go => go.Matches(dgo)))
                    .Select(go => go.Clone()));
        }

        public string AnimatorStateName => string.IsNullOrEmpty(animatorStateNameOverride)
            ? name
            : animatorStateNameOverride;

        public bool HasIdenticalBlendShape(AnimatableBlendShape blendShape)
            => blendShapes.Any(bs =>
                bs.skinnedMeshRenderer == blendShape.skinnedMeshRenderer
                && bs.name == blendShape.name
                && bs.value == blendShape.value);

        public bool HasIdenticalGameObject(AnimatableGameObject gameObject)
            => gameObjects.Any(go =>
                go.gameObject == gameObject.gameObject
                && go.active == gameObject.active);

        [Serializable]
        public class AnimatableBlendShape
        {
            public SkinnedMeshRenderer skinnedMeshRenderer;
            public string name;
            public float value;

            public bool Matches(AnimatableBlendShape blendShape)
                => skinnedMeshRenderer == blendShape.skinnedMeshRenderer && name == blendShape.name;

            public AnimatableBlendShape Clone() => new AnimatableBlendShape()
            {
                skinnedMeshRenderer = skinnedMeshRenderer,
                name = name,
                value = value
            };
        }

        [Serializable]
        public class AnimatableGameObject
        {
            public GameObject gameObject;
            //the path will only be used for finding a new GameObject if the original is deleted or something
            //TODO: will also need a way to keep it up-to-date if things get renamed
            //  perhaps [UnityEditor.InitializeOnLoad] and EditorSceneManager.activeSceneChangedInEditMode?
            //TODO: prob should make it private, still serialized, and accessible thru more controlled means
            public string path;
            public bool active;

            public AnimatableGameObject Clone() => new AnimatableGameObject()
            {
                gameObject = gameObject,
                path = path,
                active = active
            };

            internal bool Matches(AnimatableGameObject gameObject)
                => this.gameObject == gameObject.gameObject;
        }
    }
}
#endif
