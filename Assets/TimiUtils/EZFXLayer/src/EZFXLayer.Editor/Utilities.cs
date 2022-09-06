namespace EZFXLayer
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public static class Utilities
    {
        public static string GetRelativePath(this GameObject gameObject)
        {
            List<string> names = new List<string>();
            while (gameObject != null && gameObject.GetComponent<VRCAvatarDescriptor>() == null)
            {
                names.Add(gameObject.name);
                gameObject = gameObject.transform.parent.gameObject;
            }
            names.Reverse();
            string result = string.Join("/", names);
            return result;
        }

        //it's a design choice to basically not create assets in the generator and let its driver adapter do that
        //but this is fine to do if we can, and it incidentally matches the internal code of AnimatorController
        public static void TryAddObjectToAsset(Object objectToAdd, Object potentialAsset)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(potentialAsset))) return;
            AssetDatabase.AddObjectToAsset(objectToAdd, potentialAsset);
        }
    }
}
