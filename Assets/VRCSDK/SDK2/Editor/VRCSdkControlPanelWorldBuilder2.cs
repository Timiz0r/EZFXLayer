using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.BuildPipeline;
using VRCSDK2.Editor;
using Object = UnityEngine.Object;

[assembly: VRCSdkControlPanelBuilder(typeof(VRCSdkControlPanelWorldBuilder2))]

namespace VRCSDK2.Editor
{
    public class VRCSdkControlPanelWorldBuilder2 : VRCSdkControlPanelWorldBuilder
    {
        public override void ShowSettingsOptions()
        {
            GUILayout.Label("SDK2 Builder");
        }

        public override bool IsValidBuilder(out string message)
        {
            bool result = base.IsValidBuilder(out message);
            message = "A VRC_SceneDescriptor or VRC_AvatarDescriptor\nis required to build VRChat SDK Content";
            return result;
        }

        public override void SelectAllComponents()
        {
            Debug.Log("SelectAllComponents");
        }

        public override void OnGUIScene()
        {
            GUILayout.Label("", VRCSdkControlPanel.scrollViewSeparatorStyle);

            _builderScrollPos = GUILayout.BeginScrollView(_builderScrollPos, false, false, GUIStyle.none,
                GUI.skin.verticalScrollbar, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth),
                GUILayout.MinHeight(217));

            GUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.Space();
            GUILayout.Label("Local Testing", VRCSdkControlPanel.infoGuiStyle);
            GUILayout.Label(
                "Before uploading your world you may build and test it in the VRChat client. You won't be able to invite anyone from online but you can launch multiple of your own clients.",
                VRCSdkControlPanel.infoGuiStyle);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.Space();
            VRCSettings.NumClients = EditorGUILayout.IntField("Number of Clients", VRCSettings.NumClients, GUILayout.MaxWidth(190));
            EditorGUILayout.Space();
            VRCSettings.ForceNoVR = EditorGUILayout.Toggle("Force Non-VR",  VRCSettings.ForceNoVR, GUILayout.MaxWidth(190));
            EditorGUILayout.Space();

            GUI.enabled = _builder.NoGuiErrorsOrIssues();

            string lastUrl = VRC_SdkBuilder.GetLastUrl();
            
            bool lastBuildPresent = lastUrl != null;
            if (lastBuildPresent == false)
                GUI.enabled = false;
            if (VRCSettings.DisplayAdvancedSettings)
            {
                if (GUILayout.Button("Last Build"))
                {
                    VRC_SdkBuilder.shouldBuildUnityPackage = false;
                    VRC_SdkBuilder.RunLastExportedSceneResource();
                }

                if (VRC.Core.APIUser.CurrentUser.hasSuperPowers)
                {
                    if (GUILayout.Button("Copy Test URL"))
                    {
                        TextEditor te = new TextEditor {text = lastUrl};
                        te.SelectAll();
                        te.Copy();
                    }
                }
            }

            GUI.enabled = _builder.NoGuiErrorsOrIssues() ||
                          VRC.Core.APIUser.CurrentUser.developerType == VRC.Core.APIUser.DeveloperType.Internal;

#if UNITY_ANDROID
        EditorGUI.BeginDisabledGroup(true);
#endif
            if (GUILayout.Button("Build & Test"))
            {
                bool buildTestBlocked = !VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene);
                if (!buildTestBlocked)
                {
                    EnvConfig.ConfigurePlayerSettings();
                    VRC_SdkBuilder.shouldBuildUnityPackage = false;
                    AssetExporter.CleanupUnityPackageExport(); // force unity package rebuild on next publish
                    VRC_SdkBuilder.PreBuildBehaviourPackaging();
                    VRC_SdkBuilder.ExportSceneResourceAndRun();
                }
            }
#if UNITY_ANDROID
        EditorGUI.EndDisabledGroup();
#endif

            GUILayout.EndVertical();

            if (Event.current.type != EventType.Used)
            {
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            GUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.Space();
            GUILayout.Label("Online Publishing", VRCSdkControlPanel.infoGuiStyle);
            GUILayout.Label(
                "In order for other people to enter your world in VRChat it must be built and published to our game servers.",
                VRCSdkControlPanel.infoGuiStyle);
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.Space();

            if (lastBuildPresent == false)
                GUI.enabled = false;
            if (VRCSettings.DisplayAdvancedSettings)
            {
                if (GUILayout.Button("Last Build"))
                {
                    if (VRC.Core.APIUser.CurrentUser.canPublishWorlds)
                    {
                        EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", false);
                        VRC_SdkBuilder.shouldBuildUnityPackage = VRCSdkControlPanel.FutureProofPublishEnabled;
                        VRC_SdkBuilder.UploadLastExportedSceneBlueprint();
                    }
                    else
                    {
                        VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                    }
                }
            }

            GUI.enabled = _builder.NoGuiErrorsOrIssues() ||
                          VRC.Core.APIUser.CurrentUser.developerType == VRC.Core.APIUser.DeveloperType.Internal;
            if (GUILayout.Button(VRCSdkControlPanel.GetBuildAndPublishButtonString()))
            {
                bool buildBlocked = !VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene);
                if (!buildBlocked)
                {
                    if (VRC.Core.APIUser.CurrentUser.canPublishWorlds)
                    {
                        EnvConfig.ConfigurePlayerSettings();
                        EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", false);
                        
                        VRC_SdkBuilder.shouldBuildUnityPackage = VRCSdkControlPanel.FutureProofPublishEnabled;
                        VRC_SdkBuilder.PreBuildBehaviourPackaging();
                        VRC_SdkBuilder.ExportAndUploadSceneBlueprint();
                    }
                    else
                    {
                        VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                    }
                }
            }

            GUILayout.EndVertical();
            GUI.enabled = true;

            if (Event.current.type == EventType.Used) return;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        protected override void OnGUISceneCheck(VRC.SDKBase.VRC_SceneDescriptor scene)
        {
            base.OnGUISceneCheck(scene);
            
            foreach (VRC_DataStorage ds in Object.FindObjectsOfType<VRC_DataStorage>())
            {
                VRCSDK2.VRC_ObjectSync os = ds.GetComponent<VRCSDK2.VRC_ObjectSync>();
                if (os != null && os.SynchronizePhysics)
                    _builder.OnGUIWarning(scene, ds.name + " has a VRC_DataStorage and VRC_ObjectSync, with SynchronizePhysics enabled.",
                        delegate { Selection.activeObject = os.gameObject; }, null);
            }
        }
    }
}