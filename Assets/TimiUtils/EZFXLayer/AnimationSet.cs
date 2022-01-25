namespace TimiUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
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

        public bool showBlendShapes = true;
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public bool showGameObjects = true;
        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();

        //overrides the AnimatorLayer's one. tho, typically, one wouldn't do this; just clarifying behavior in case done.
        public string menuPath = null;

        public string AnimatorStateName => animatorStateNameOverride ?? name;

        [Serializable]
        public class AnimatableBlendShape
        {
            public SkinnedMeshRenderer skinnedMeshRenderer;
            public string name;
            public float value;
        }

        [Serializable]
        public class AnimatableGameObject
        {
            public GameObject gameObject;
            //the path will only be used for finding a new GameObject if the original is deleted or something
            //TODO: will also need a way to keep it up-to-date if things get renamed
            public string path;
            public bool active;
        }
    }
}
