namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    //this and animatables could hypothetically go in the components assembly, and they used to be there
    //they were moved here when merging an identical looking class. in this merge, we couldn't simply keep it with
    //the components because there's editor code (and related) here.
    //if we do want to move it back, we'd just need to move the behaviors out of this class
    //likely no problem with the current setup during play mode, and definitely no problem during upload
    public class AnimationConfiguration
    {
        //TODO: encapsulate stuff more
        public string name;
        public string animatorStateNameOverride;
        public string toggleNameOverride;
        public bool isDefaultState;
        public bool isDefaultAnimation;
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();
        public bool isFoldedOut = true;
    }
}
