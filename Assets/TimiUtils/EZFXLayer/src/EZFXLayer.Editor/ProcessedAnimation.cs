namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;

    public class ProcessedAnimation
    {
        private readonly string stateName;
        //not a huge fan of such mutations, but it's convenient and the logic flows naturally
        private AnimatorState correspondingState = null;

        public AnimatorState CorrespondingState => correspondingState ?? throw new InvalidOperationException(
            "Somehow AddState didn't produce a CorrespondingState, or AddState wasn't called beforehand.");
        public int Index { get; }
        public bool IsToBeDefaultState { get; }

        public ProcessedAnimation(string stateName, int index, bool isToBeDefaultState)
        {
            this.stateName = stateName;
            Index = index;
            IsToBeDefaultState = isToBeDefaultState;
        }

        public bool MatchesState(AnimatorState state)
            => state.name.Equals(stateName, StringComparison.OrdinalIgnoreCase);

        public void AddState(AnimatorController controller, List<AnimatorState> states, ref AnimatorState defaultState)
        {
            correspondingState = states.SingleOrDefault(
                s => s.name.Equals(stateName, StringComparison.OrdinalIgnoreCase));
            if (correspondingState != null) return;
            correspondingState = new AnimatorState()
            {
                hideFlags = HideFlags.HideInHierarchy,
                name = stateName
            };
            Utilities.TryAddObjectToAsset(correspondingState, controller);
            states.Add(correspondingState);

            defaultState = IsToBeDefaultState ? correspondingState : defaultState;
        }
    }
}
