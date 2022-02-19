namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;

    public class TestVrcAvatar : IVrcAvatar
    {
        public IEnumerable<TestVrcAvatar> ToEnumerable() => Enumerable.Repeat(this, 1);
    }
}
