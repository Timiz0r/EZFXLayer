namespace EZFXLayer
{
    using System.Linq;
    using UnityEngine;
    using VRC.SDKBase.Editor.BuildPipeline;

    internal class BuildPipelineProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => 0;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            ReferenceComponent[] referenceComponents =
                avatarGameObject.GetComponentsInChildren<ReferenceComponent>(includeInactive: true);

            AnimatorLayerComponent[] layerComponents =
                avatarGameObject.GetComponentsInChildren<AnimatorLayerComponent>(includeInactive: true);

            //TODO: ofc use them to generate if autogen is enabled

            foreach (Component component in referenceComponents.Union<Component>(layerComponents))
            {
                Object.DestroyImmediate(component);
            }

            return true;
        }
    }
}
