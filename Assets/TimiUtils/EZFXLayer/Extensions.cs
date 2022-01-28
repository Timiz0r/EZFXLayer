namespace TimiUtils.EZFXLayer
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public static class Extensions
    {
        public static IEnumerable<string> GetBlendShapeNames(this Mesh mesh)
            => Enumerable.Range(0, mesh.blendShapeCount).Select(i => mesh.GetBlendShapeName(i));
    }
}
