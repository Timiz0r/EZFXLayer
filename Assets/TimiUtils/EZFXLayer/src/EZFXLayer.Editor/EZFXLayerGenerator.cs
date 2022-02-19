namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class EZFXLayerGenerator
    {
        public void Generate(
            IEnumerable<GameObject> avatars,
            AnimatorController fxController,
            VRCExpressionsMenu menu,
            VRCExpressionParameters parameters)
        {
            PreValidate(avatars);
        }

        private void PreValidate(IEnumerable<GameObject> avatars)
        {
            foreach (GameObject avatar in avatars)
            {
                if (avatar.GetComponentInChildren<VRCAvatarDescriptor>() == null)
                {
                    throw new InvalidOperationException(
                        $"Avatar '{avatar.name}' has no {nameof(VRCAvatarDescriptor)}.");
                }
            }
        }
    }
}
