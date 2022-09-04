namespace EZFXLayer
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public static class Extensions
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
    }
}
