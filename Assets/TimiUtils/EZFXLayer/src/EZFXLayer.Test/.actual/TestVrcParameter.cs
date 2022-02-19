namespace EZFXLayer.Test
{
    using System;

    public class TestVrcParameter : IVrcParameter
    {
        private TestVrcParameter()
        {

        }

        public static TestVrcParameter FromEmpty() => new();
    }
}
