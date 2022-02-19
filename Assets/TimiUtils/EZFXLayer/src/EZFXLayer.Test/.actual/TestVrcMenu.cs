namespace EZFXLayer.Test
{
    using System;

    public class TestVrcMenu : IVrcMenu
    {
        private TestVrcMenu()
        {

        }

        public static TestVrcMenu FromEmpty() => new();
    }
}
