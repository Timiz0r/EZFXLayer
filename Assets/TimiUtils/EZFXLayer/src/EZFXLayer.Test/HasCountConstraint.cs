namespace EZFXLayer.Test
{
    using System.Collections;
    using System.Linq;
    using NUnit.Framework.Constraints;

    public class HasCountConstraint : CollectionConstraint
    {
        private readonly int targetCount;
        private readonly IEnumerable targetCollection;

        public HasCountConstraint(int targetCount)
        {
            this.targetCount = targetCount;
        }
        public HasCountConstraint(IEnumerable targetCollection)
        {
            this.targetCollection = targetCollection;
        }

        protected override bool Matches(IEnumerable collection)
            => collection.Cast<object>().Count() == GetTargetCount();

        public override string Description
            => GetTargetCount() is var c && c == 0
                ? "no item"
                : c == 1
                    ? "exactly one item"
                    : $"exactly {c} items";

        private int GetTargetCount() => targetCollection?.Cast<object>()?.Count() ?? targetCount;

        public static Constraint Create(int targetCount)
            => new ConstraintExpression().Append(new HasCountConstraint(targetCount));
        public static Constraint Create(IEnumerable targetCollection)
            => new ConstraintExpression().Append(new HasCountConstraint(targetCollection));
    }
}
