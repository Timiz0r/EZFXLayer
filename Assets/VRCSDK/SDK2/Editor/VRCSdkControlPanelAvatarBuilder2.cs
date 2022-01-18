using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation.Performance.Stats;
using VRCSDK2.Editor;
using VRCSDK2.Validation;

[assembly: VRCSdkControlPanelBuilder(typeof(VRCSdkControlPanelAvatarBuilder2))]

namespace VRCSDK2.Editor
{
    public class VRCSdkControlPanelAvatarBuilder2 : VRCSdkControlPanelAvatarBuilder
    {
        public override void ValidateFeatures(VRC.SDKBase.VRC_AvatarDescriptor avatar, Animator anim, AvatarPerformanceStats perfStats)
        {
            List<Component> componentsToRemove = AvatarValidation.FindIllegalComponents(avatar.gameObject).ToList();

            // create a list of the PipelineSaver component(s)
            List<Component> toRemoveSilently = new List<Component>();
            foreach (Component c in componentsToRemove)
            {
                if (c.GetType().Name == "PipelineSaver")
                {
                    toRemoveSilently.Add(c);
                }
            }

            // delete PipelineSaver(s) from the list of the Components we will destroy now
            foreach (Component c in toRemoveSilently)
            {
                    componentsToRemove.Remove(c);
            }

            HashSet<string> componentsToRemoveNames = new HashSet<string>();
            List<Component> toRemove = componentsToRemove as List<Component> ?? componentsToRemove;
            foreach (Component c in toRemove)
            {
                if (componentsToRemoveNames.Contains(c.GetType().Name) == false)
                    componentsToRemoveNames.Add(c.GetType().Name);
            }

            if (componentsToRemoveNames.Count > 0)
                _builder.OnGUIError(avatar,
                    "The following component types are found on the Avatar and will be removed by the client: " +
                    string.Join(", ", componentsToRemoveNames.ToArray()),
                    delegate { ShowRestrictedComponents(toRemove); },
                    delegate { FixRestrictedComponents(toRemove); });

            List<AudioSource> audioSources =
                avatar.gameObject.GetComponentsInChildren<AudioSource>(true).ToList();
            if (audioSources.Count > 0)
                _builder.OnGUIWarning(avatar,
                    "Audio sources found on Avatar, they will be adjusted to safe limits, if necessary.",
                    GetAvatarSubSelectAction(avatar, typeof(AudioSource)), null);

            List<VRCStation> stations =
                avatar.gameObject.GetComponentsInChildren<VRCStation>(true).ToList();
            if (stations.Count > 0)
                _builder.OnGUIWarning(avatar, "Stations found on Avatar, they will be adjusted to safe limits, if necessary.",
                    GetAvatarSubSelectAction(avatar, typeof(VRCStation)), null);

            if (VRCSdkControlPanel.HasSubstances(avatar.gameObject))
            {
                _builder.OnGUIWarning(avatar,
                    "This avatar has one or more Substance materials, which is not supported and may break in-game. Please bake your Substances to regular materials.",
                    () => { Selection.objects = VRCSdkControlPanel.GetSubstanceObjects(avatar.gameObject); },
                    null);
            }

            CheckAvatarMeshesForLegacyBlendShapesSetting(avatar);
            CheckAvatarMeshesForMeshReadWriteSetting(avatar);

#if UNITY_ANDROID
        IEnumerable<Shader> illegalShaders = AvatarValidation.FindIllegalShaders(avatar.gameObject);
        foreach (Shader s in illegalShaders)
        {
            _builder.OnGUIError(avatar, "Avatar uses unsupported shader '" + s.name + "'. You can only use the shaders provided in 'VRChat/Mobile' for Quest avatars.", delegate () { Selection.activeObject
 = avatar.gameObject; }, null);
        }
#endif

            foreach (AvatarPerformanceCategory perfCategory in Enum.GetValues(typeof(AvatarPerformanceCategory)))
            {
                if (perfCategory == AvatarPerformanceCategory.Overall ||
                    perfCategory == AvatarPerformanceCategory.PolyCount ||
                    perfCategory == AvatarPerformanceCategory.AABB ||
                    perfCategory == AvatarPerformanceCategory.AvatarPerformanceCategoryCount)
                {
                    continue;
                }

                Action show = null;

                switch (perfCategory)
                {
                    case AvatarPerformanceCategory.AnimatorCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(Animator));
                        break;
                    case AvatarPerformanceCategory.AudioSourceCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(AudioSource));
                        break;
                    case AvatarPerformanceCategory.BoneCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(SkinnedMeshRenderer));
                        break;
                    case AvatarPerformanceCategory.ClothCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(Cloth));
                        break;
                    case AvatarPerformanceCategory.ClothMaxVertices:
                        show = GetAvatarSubSelectAction(avatar, typeof(Cloth));
                        break;
                    case AvatarPerformanceCategory.LightCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(Light));
                        break;
                    case AvatarPerformanceCategory.LineRendererCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(LineRenderer));
                        break;
                    case AvatarPerformanceCategory.MaterialCount:
                        show = GetAvatarSubSelectAction(avatar,
                            new[] {typeof(MeshRenderer), typeof(SkinnedMeshRenderer)});
                        break;
                    case AvatarPerformanceCategory.MeshCount:
                        show = GetAvatarSubSelectAction(avatar,
                            new[] {typeof(MeshRenderer), typeof(SkinnedMeshRenderer)});
                        break;
                    case AvatarPerformanceCategory.ParticleCollisionEnabled:
                        show = GetAvatarSubSelectAction(avatar, typeof(ParticleSystem));
                        break;
                    case AvatarPerformanceCategory.ParticleMaxMeshPolyCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(ParticleSystem));
                        break;
                    case AvatarPerformanceCategory.ParticleSystemCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(ParticleSystem));
                        break;
                    case AvatarPerformanceCategory.ParticleTotalCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(ParticleSystem));
                        break;
                    case AvatarPerformanceCategory.ParticleTrailsEnabled:
                        show = GetAvatarSubSelectAction(avatar, typeof(ParticleSystem));
                        break;
                    case AvatarPerformanceCategory.PhysicsColliderCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(Collider));
                        break;
                    case AvatarPerformanceCategory.PhysicsRigidbodyCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(Rigidbody));
                        break;
                    case AvatarPerformanceCategory.PolyCount:
                        show = GetAvatarSubSelectAction(avatar,
                            new[] {typeof(MeshRenderer), typeof(SkinnedMeshRenderer)});
                        break;
                    case AvatarPerformanceCategory.SkinnedMeshCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(SkinnedMeshRenderer));
                        break;
                    case AvatarPerformanceCategory.TrailRendererCount:
                        show = GetAvatarSubSelectAction(avatar, typeof(TrailRenderer));
                        break;
                }

                // we can only show these buttons if DynamicBone is installed

                Type dynamicBoneType = typeof(AvatarValidation).Assembly.GetType("DynamicBone");
                Type dynamicBoneColliderType = typeof(AvatarValidation).Assembly.GetType("DynamicBoneCollider");
                if ((dynamicBoneType != null) && (dynamicBoneColliderType != null))
                {
                    switch (perfCategory)
                    {
                        case AvatarPerformanceCategory.DynamicBoneColliderCount:
                            show = GetAvatarSubSelectAction(avatar, dynamicBoneColliderType);
                            break;
                        case AvatarPerformanceCategory.DynamicBoneCollisionCheckCount:
                            show = GetAvatarSubSelectAction(avatar, dynamicBoneColliderType);
                            break;
                        case AvatarPerformanceCategory.DynamicBoneComponentCount:
                            show = GetAvatarSubSelectAction(avatar, dynamicBoneType);
                            break;
                        case AvatarPerformanceCategory.DynamicBoneSimulatedBoneCount:
                            show = GetAvatarSubSelectAction(avatar, dynamicBoneType);
                            break;
                    }
                }

                OnGUIPerformanceInfo(avatar, perfStats, perfCategory, show, null);
            }

            _builder.OnGUILink(avatar, "Avatar Optimization Tips", VRCSdkControlPanel.AVATAR_OPTIMIZATION_TIPS_URL);
        }

        public override void OnGUIAvatar(VRC.SDKBase.VRC_AvatarDescriptor avatar)
        {
            EditorGUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.Space();

            GUILayout.Label("Online Publishing", VRCSdkControlPanel.infoGuiStyle);
            GUILayout.Label(
                "In order for other people to see your avatar in VRChat it must be built and published to our game servers.",
                VRCSdkControlPanel.infoGuiStyle);

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.Space();

            GUI.enabled = _builder.NoGuiErrorsOrIssues() ||
                          VRC.Core.APIUser.CurrentUser.developerType == VRC.Core.APIUser.DeveloperType.Internal;
            if (GUILayout.Button(VRCSdkControlPanel.GetBuildAndPublishButtonString()))
            {
                bool buildBlocked = !VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Avatar);
                if (!buildBlocked)
                {
                    if (VRC.Core.APIUser.CurrentUser.canPublishAvatars)
                    {
                        EnvConfig.FogSettings originalFogSettings = EnvConfig.GetFogSettings();
                        EnvConfig.SetFogSettings(
                            new EnvConfig.FogSettings(EnvConfig.FogSettings.FogStrippingMode.Custom, true, true, true));

#if UNITY_ANDROID
                        EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", true);
#else
                        EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", false);
#endif

                        VRC_SdkBuilder.shouldBuildUnityPackage = VRCSdkControlPanel.FutureProofPublishEnabled;
                        VRC_SdkBuilder.ExportAndUploadAvatarBlueprint(avatar.gameObject);

                        EnvConfig.SetFogSettings(originalFogSettings);

                        // this seems to workaround a Unity bug that is clearing the formatting of two levels of Layout
                        // when we call the upload functions
                        return;
                    }
                    else
                    {
                        VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUI.enabled = true;
        }
    }
}
