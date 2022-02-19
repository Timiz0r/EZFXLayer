namespace EZFXLayer.Test
{
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class VrcAssets
    {
        private TestFXController startingFXController = TestFXController.FromEmpty();
        private TestVrcMenu startMenu = TestVrcMenu.FromEmpty();
        private TestVrcParameter startingParameters = TestVrcParameter.FromEmpty();

        public TestFXController FXController { get; } = TestFXController.FromEmpty();
        public TestVrcMenu Menu { get; } = TestVrcMenu.FromEmpty();
        public TestVrcParameter Parameters { get; } = TestVrcParameter.FromEmpty();


    }
}
