namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public static class VrcAvatar
    {
        public static IReadOnlyList<GameObject> Create(string name) => Create(1, name);
        public static IReadOnlyList<GameObject> Create(int count, string name)
            => Enumerable.Range(0, count)
                .Select(i => new GameObject(
                    $"{name}{(i == 0 ? "" : i.ToString(CultureInfo.InvariantCulture))}",
                    typeof(VRCAvatarDescriptor)))
                .ToArray();
    }
}
