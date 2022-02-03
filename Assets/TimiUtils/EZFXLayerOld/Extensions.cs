#if UNITY_EDITOR
namespace TimiUtils.EZFXLayerOld
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public static class Extensions
    {
        public static IEnumerable<string> GetBlendShapeNames(this Mesh mesh)
            => Enumerable.Range(0, mesh.blendShapeCount).Select(i => mesh.GetBlendShapeName(i));

        public static string GetRelativePath(this GameObject gameObject)
        {
            List<string> names = new List<string>();
            while (gameObject != null && gameObject.GetComponent<VRCAvatarDescriptor>() == null)
            {
                names.Add(gameObject.name);
                gameObject = gameObject.transform.parent.gameObject;
            }
            names.Reverse();
            var result = string.Join("/", names);
            return result;
        }
    }
}
#endif
