namespace EZFXLayer.Test
{
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public static class Avatar
    {
        public static GameObject Create(string name) => new(name, typeof(VRCAvatarDescriptor));
    }
}
