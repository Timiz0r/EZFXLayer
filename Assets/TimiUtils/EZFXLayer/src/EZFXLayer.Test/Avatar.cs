namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public static class Avatar
    {
        public static IEnumerable<GameObject> Create(string name)
            => new[] { new GameObject(name, typeof(VRCAvatarDescriptor)) };
    }
}
