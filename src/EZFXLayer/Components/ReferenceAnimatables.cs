namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class ReferenceAnimatables
    {
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();
        public bool isFoldedOut = true;
    }
}
