namespace EZUtils.EZFXLayer
{
    using UnityEngine;

    internal interface IAnimatableConfigurator
    {
        //permanency is meant for animations that have a blendshape that corresponds to a reference blendshape
        bool IsBlendShapeSelected(SkinnedMeshRenderer smr, string name, out bool permanent, out string key);
        void AddBlendShape(AnimatableBlendShape blendShape);
        void RemoveBlendShape(AnimatableBlendShape blendShape);

        void AddGameObject(AnimatableGameObject gameObject);
        void RemoveGameObject(AnimatableGameObject gameObject);
    }
}
