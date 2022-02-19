namespace EZFXLayer.Test
{
    using System;

    public class TestFXController : IFXController
    {
        public static TestFXController FromEmpty() => new TestFXController();
    }
}
