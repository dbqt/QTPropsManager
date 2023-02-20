#if UNITY_EDITOR
using AnimatorAsCode.V0;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using AnimatorController = UnityEditor.Animations.AnimatorController;

namespace QTAssets
{
    public class QTPropsManager : MonoBehaviour
    {
        public VRCAvatarDescriptor Avatar;
        public QTProps Data;

        public AnimatorController AssetContainer;
        [Tooltip("(Optional) This will use the root menu if not assigned.")]
        public VRCExpressionsMenu menuLocation;
        public bool UseWriteDefault;
        public List<QTProp> Props;

        private VRCAvatarDescriptor previousAvatar = null;

        private void OnValidate()
        {
            // New avatar assigned
            if (previousAvatar != Avatar && Avatar != null)
            {
                // Load or create data asset
                var path = QTHelpers.EnsureBaseFolderExists(Avatar) + "QTPropsData.asset";
                if (QTHelpers.CheckAssetExists(path))
                {
                    var data = AssetDatabase.LoadAssetAtPath<QTProps>(path);
                    Data = data;
                    if (LoadFromAsset())
                    {
                        Debug.Log($"[QTPropsManager] Loaded data from {path}");
                    }
                    else
                    {
                        Debug.LogWarning($"[QTPropsManager] Failed to load data from {path}");
                    }
                }
                else
                {
                    var data = ScriptableObject.CreateInstance<QTProps>();
                    AssetDatabase.CreateAsset(data, QTHelpers.EnsureBaseFolderExists(Avatar) + "QTPropsData.asset");
                    Data = data;
                    if (SaveToAsset())
                    {
                        Debug.Log($"[QTPropsManager] Saved data to {path}");
                    }
                    else
                    {
                        Debug.LogWarning($"[QTPropsManager] Failed to save data to {path}");
                    }
                }

                previousAvatar = Avatar;
            }

            // Removed avatar, clear prefab
            if (Avatar == null)
            {
                previousAvatar = null;
                Data = null;
                Props = null;
                menuLocation = null;
            }

            // No data yet, create one
            if (Avatar != null && Data == null)
            {
                var data = ScriptableObject.CreateInstance<QTProps>();
                AssetDatabase.CreateAsset(data, QTHelpers.EnsureBaseFolderExists(Avatar) + "QTPropsData.asset");
                Data = data; 
                SaveToAsset();
            }

            // Setup asset container
            if (Avatar != null && Data != null && Data.AssetContainer == null)
            {
                Data.SetupAssetContainer(Avatar);
                LoadFromAsset();
            }
        }

        /// <summary>
        /// Saves data to asset using avatar descriptor
        /// </summary>
        public bool SaveToAsset()
        {
            if (Avatar != null && Data != null)
            {
                Data.AssetContainer = AssetContainer;
                Data.menuLocation = menuLocation;
                Data.UseWriteDefault = UseWriteDefault;
                Data.Props = Props;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads data from asset using avatar descriptor
        /// </summary>
        public bool LoadFromAsset()
        {
            if (Avatar != null && Data != null)
            {
                AssetContainer = Data.AssetContainer;
                menuLocation = Data.menuLocation;
                UseWriteDefault = Data.UseWriteDefault;
                Props = Data.Props;

                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class QTProp
    {
        public string Name = "";
        public bool DefaultState;
        public bool Saved;
        public GameObject PropObject;
        public Transform BoneToAttachPropTo;
        public string AssetKey;
        public Texture2D Icon;

        public bool IsValid() =>
            !string.IsNullOrEmpty(Name)
            && PropObject != null
            && BoneToAttachPropTo != null;
    }

    [CustomEditor(typeof(QTPropsManager), true)]
    public class QTPropsManagerEditor : Editor
    {
        private const string Prefix = "QTProps_";
        private ReorderableList list;

        private SerializedProperty serializedAvatar;
        private SerializedProperty serializedMenu;
        private SerializedProperty serializedWD;

        AnimBool showPropsFields;

        private int installingProgress = 0;

        private void OnEnable()
        {
            var my = (QTPropsManager)target;

            // Grab basic properties
            serializedAvatar = serializedObject.FindProperty("Avatar");
            var serializedList = serializedObject.FindProperty("Props");
            serializedMenu = serializedObject.FindProperty("menuLocation");
            serializedWD = serializedObject.FindProperty("UseWriteDefault");

            // Visibility toggle for the list of props
            showPropsFields = new AnimBool(true);
            showPropsFields.valueChanged.AddListener(Repaint);

            // Define the look of the list of props
            list = new ReorderableList(serializedObject, serializedList, true, false, true, true);
            list.elementHeight = EditorGUIUtility.singleLineHeight * 7 + EditorGUIUtility.standardVerticalSpacing;
            list.onAddCallback = list =>
            {
                if (my.Data == null) return;

                my.Props.Add(new QTProp());
            };
            list.onChangedCallback = list =>
            {
                my.SaveToAsset();
            };
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (my.Data == null) return;

                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                var thisProp = my.Props[index];
                // Red if invalid
                if (!thisProp.IsValid())
                {
                    var style = new GUIStyle(GUI.skin.box);
                    var texture = new Texture2D(2, 2);
                    var color = new Color(1f, 0f, 0f, 0.5f);
                    texture.SetPixels(new Color[4] { color, color, color, color });
                    texture.Apply();
                    style.normal.background = texture;
                    GUI.Box(new Rect(rect.x, rect.y, rect.width, rect.height), "", style);
                }
                // Yellow if share name with another
                else if (my.Props.Any(prop => prop != thisProp && prop.Name.Equals(thisProp.Name)))
                {
                    var style = new GUIStyle(GUI.skin.box);
                    var texture = new Texture2D(2, 2);
                    var color = new Color(1f, 1f, 0f, 0.2f);
                    texture.SetPixels(new Color[4] { color, color, color, color });
                    texture.Apply();
                    style.normal.background = texture;
                    GUI.Box(new Rect(rect.x, rect.y, rect.width, rect.height), "", style);
                }

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + EditorGUIUtility.standardVerticalSpacing, EditorGUIUtility.labelWidth * 2, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Name"), new GUIContent("Unique name (required)"));

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2, EditorGUIUtility.labelWidth * 2, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("PropObject"), new GUIContent("Prop GameObject (required)"));

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 3, EditorGUIUtility.labelWidth * 2, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("BoneToAttachPropTo"), new GUIContent("Bone to attach to (required)"));

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 4, EditorGUIUtility.labelWidth * 2, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Icon"), new GUIContent("Menu toggle icon (optional)"));

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 5, EditorGUIUtility.labelWidth * 2, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("DefaultState"), new GUIContent("Default toggle state"));

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 5 + EditorGUIUtility.standardVerticalSpacing * 6, EditorGUIUtility.labelWidth * 2, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Saved"), new GUIContent("Save toggle state"));
                /* GUI.enabled = false;
                 EditorGUI.PropertyField(
                     new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 5 + EditorGUIUtility.standardVerticalSpacing * 6, EditorGUIUtility.labelWidth * 2, EditorGUIUtility.singleLineHeight),
                     element.FindPropertyRelative("AssetKey"), GUIContent.none);
                 GUI.enabled = true;*/

            };
        }

        public override void OnInspectorGUI()
        {
            var my = (QTPropsManager)target;

            serializedObject.Update();

            // Header
            GUILayout.Box("QT Props Manager", EditorStyles.largeLabel);
            EditorGUI.indentLevel++;

            // Avatar descriptor
            EditorGUILayout.PropertyField(serializedAvatar, new GUIContent("Avatar (required)"));

            // Hide UI if avatar is not set and data was not loaded from that
            if (my.Data == null)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Basic properties
            EditorGUILayout.PropertyField(serializedMenu, new GUIContent("VRC Submenu parent (optional)"));
            EditorGUILayout.PropertyField(serializedWD);

            GUILayout.Space(20);
            EditorGUI.indentLevel++;

            // Foldout for the list of props
            showPropsFields.target = EditorGUILayout.Foldout(showPropsFields.target, "Props");
            if (EditorGUILayout.BeginFadeGroup(showPropsFields.faded))
            {
                list.DoLayoutList();
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
            GUILayout.Space(20);

            // If last step of installing
            if (installingProgress == 1)
            {
                var container = my.Avatar.transform.GetChildByName("QTProps").gameObject;

                // Toggle all the prop objects to the state they are supposed to be in by default
                foreach (var prop in container.transform.GetChildren())
                {
                    bool defaultState = my.Props.FirstOrDefault(p => p.Name.Equals(prop.gameObject.name))?.DefaultState ?? false;
                    prop.gameObject.SetActive(defaultState);
                }

                // Clear progress bar and show dialog
                installingProgress--;
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Done", "Installation completed!", "Ok");
            }

            // HAX: Delaying installing to give time to the editor to setup the constraint
            if (installingProgress > 1)
            {
                EditorUtility.DisplayProgressBar("Installing QTProps", "Installing...", (60f - installingProgress) / 60f);
                installingProgress--;
                Repaint();
            }

            // Install
            if (GUILayout.Button("Install / Update"))
            {
                // Abort if same name or invalid prop
                bool sameNames = my.Props.GroupBy(prop => prop.Name).Any(group => group.Count() > 1);
                bool invalidProp = my.Props.Any(prop => !prop.IsValid());
                if (sameNames || invalidProp)
                {
                    if (sameNames)
                    {
                        EditorUtility.DisplayDialog("Error", "Some of your props have the same name, make sure they are unique! (Yellow)", "OK");
                        Debug.LogError("[QTPropsManager] Some of your props have the same name, make sure they are unique! (Yellow)");
                    }

                    if (invalidProp)
                    {
                        EditorUtility.DisplayDialog("Error", "Some of your props are invalid, double check them! (Red)", "OK");
                        Debug.LogError("[QTPropsManager] Some of your props are invalid, double check them! (Red)");
                    }
                    return;
                }

                // Check menu to see if there is enough space
                var menu = my.menuLocation ?? my.Avatar.expressionsMenu;
                if (menu != null && menu.controls != null && menu.controls.Count > 7)
                {
                    EditorUtility.DisplayDialog("Error", "The menu doesn't have space for another control, please specify another menu!", "OK");
                    Debug.LogError("[QTPropsManager] The menu doesn't have space for another control, please specify another menu!");
                    return;
                }

                // Reset existing stuff
                UninstallProps();

                // Make sure FX animator and VRC assets exists
                QTHelpers.EnsureFXAnimatorExists(my.Avatar);
                QTHelpers.EnsureVRCMenuAndParametersExist(my.Avatar);
                my.Data.SetupAssetContainer(my.Avatar);
                my.LoadFromAsset();

                // Get QTProps object
                GameObject container;
                try
                {
                    container = my.Avatar.transform.GetChildByName("QTProps").gameObject;
                }
                catch
                {
                    // Doesn't exist, and that's okay, create it
                    container = new GameObject();
                    container.name = "QTProps";
                    container.transform.SetParent(my.Avatar.transform);
                }

                // Install!
                foreach (var prop in my.Props)
                {
                    InstallProp(prop, container);
                }

                GenerateVRCMenus();

                // Start progress bar with delay to let the constraint setup
                installingProgress = 60;
            }

            if (GUILayout.Button("Uninstall"))
            {
                UninstallProps();
                EditorUtility.DisplayDialog("Done", "Uninstalling completed!", "Ok");
            }

            serializedObject.ApplyModifiedProperties();
            my.SaveToAsset();
        }

        private void InstallProp(QTProp prop, GameObject parent)
        {
            var my = (QTPropsManager)target;

            // Make sure the asset has a unique key for the animator management
            if (string.IsNullOrEmpty(prop.AssetKey?.Trim()))
            {
                prop.AssetKey = GUID.Generate().ToString();
                serializedObject.ApplyModifiedProperties();
            }

            var aac = QTHelpers.AnimatorAsCode($"{Prefix}{prop.Name}", my.Avatar, my.AssetContainer, prop.AssetKey, my.UseWriteDefault);
            var fx = aac.CreateMainFxLayer();

            // Add prop to parent and hide original
            var newProp = Instantiate(prop.PropObject);
            newProp.name = prop.Name;
            newProp.transform.SetParent(parent.transform);
            newProp.SetActive(true);

            // Hide original if it was in the scene
            if (prop.PropObject.activeInHierarchy)
            {
                prop.PropObject.SetActive(false);
            }

            // Setup parent constraint to bone
            var constraintSource = new ConstraintSource();
            constraintSource.sourceTransform = prop.BoneToAttachPropTo;
            constraintSource.weight = 1;

            var parentConstraint = newProp.AddComponent<ParentConstraint>();
            parentConstraint.AddSource(constraintSource);
            parentConstraint.constraintActive = true;
            parentConstraint.weight = 1;

            // Setup animations & transitions
            var param = fx.BoolParameter($"{Prefix}{prop.Name}_Toggle");
            AacFlState on;
            AacFlState off;

            // Change the order to set the default state
            if (prop.DefaultState)
            {
                on = fx.NewState($"{prop.Name}_On").WithAnimation(aac.NewClip().Toggling(newProp, true));
                off = fx.NewState($"{prop.Name}_Off").WithAnimation(aac.NewClip().Toggling(newProp, false));
            }
            else
            {
                off = fx.NewState($"{prop.Name}_Off").WithAnimation(aac.NewClip().Toggling(newProp, false));
                on = fx.NewState($"{prop.Name}_On").WithAnimation(aac.NewClip().Toggling(newProp, true));
            }

            off.TransitionsTo(on).When(param.IsTrue());
            on.TransitionsTo(off).When(param.IsFalse());

            // Add VRC parameters
            QTHelpers.AddVRCParameter(my.Avatar, $"{Prefix}{prop.Name}_Toggle", prop.DefaultState, prop.Saved);
        }

        private void UninstallProps()
        {
            var my = (QTPropsManager)target;
            // Clean up props game objects
            try
            {
                var prefab = my.Avatar.transform.GetChildByName("QTProps")?.gameObject;
                if (prefab != null)
                {
                    DestroyImmediate(prefab);
                }
            }
            catch (KeyNotFoundException)
            {
                // Ignore, the prefab wasn't there, nothing to uninstall
            }

            UninstallPropsAnimatorAndVRCAssets();
        }

        private void UninstallPropsAnimatorAndVRCAssets()
        {
            var my = (QTPropsManager)target;

            // Clean up animator layers and parameters
            var animController = QTHelpers.GetAnimatorController(my.Avatar, VRCAvatarDescriptor.AnimLayerType.FX);

            QTHelpers.RemoveAnimatorLayersByName(animController, Prefix);
            if (animController != null)
            {

                QTHelpers.RemoveParametersFromAnimatorByName(animController, $"{Prefix}");
            }

            // Clean up VRC Parameters
            QTHelpers.RemoveVRCParametersByName(my.Avatar, Prefix);

            // Clean up VRC menus
            var menu = my.menuLocation ?? my.Avatar.expressionsMenu;
            var propMenu = menu.controls.FirstOrDefault(control => control.name.Equals("QTProps"));
            if (propMenu != null)
            {
                propMenu.subMenu.controls.Clear();
                menu.controls.Remove(propMenu);
            }

            QTHelpers.DeleteVRCMenuAssets(my.Avatar, "QTPropsMenu");

            // Clean up asset container for animator as code
            QTHelpers.DeleteAssetContainer(my.Avatar);
            my.AssetContainer = null;
            my.SaveToAsset();
        }

        private void GenerateVRCMenus()
        {
            var my = (QTPropsManager)target;

            // Generate VRC Menus
            var menu = my.menuLocation ?? my.Avatar.expressionsMenu;

            // Single page
            if (my.Props.Count <= 8)
            {
                var submenu = QTHelpers.CreateOrGetVRCMenuAsset(my.Avatar, "QTPropsMenu0");
                // Add toggle for each prop
                foreach (var prop in my.Props)
                {
                    submenu.controls.Add(new VRCExpressionsMenu.Control()
                    {
                        name = prop.Name,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        icon = prop.Icon,
                        parameter = new VRCExpressionsMenu.Control.Parameter() { name = $"{Prefix}{prop.Name}_Toggle" }
                    });
                }

                // Add submenu with all the props
                menu.controls.Add(new VRCExpressionsMenu.Control()
                {
                    name = "QTProps",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = submenu
                });
            }
            // Multi-page setup
            else
            {
                int page = 0;
                Queue<QTProp> queue = new Queue<QTProp>(my.Props);
                VRCExpressionsMenu previousPage = null;
                VRCExpressionsMenu currentPage = QTHelpers.CreateOrGetVRCMenuAsset(my.Avatar, $"QTPropsMenu{page}");
                VRCExpressionsMenu nextPage = null;

                do
                {
                    // Number of items on this page (7 for first page, 6 otherwise, technically also 7 on last page)
                    int numItemsThisPage = (page == 0) ? 7 : 6;

                    // If need a next page (has more props left than item slots)
                    if (queue.Count > numItemsThisPage)
                    {
                        // Add a next button
                        nextPage = QTHelpers.CreateOrGetVRCMenuAsset(my.Avatar, $"QTPropsMenu{page + 1}");
                        currentPage.controls.Add(new VRCExpressionsMenu.Control()
                        {
                            name = "Next",
                            type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = nextPage
                        });
                    }
                    else
                    {
                        // We get an extra space for being on the last page
                        numItemsThisPage++;
                    }

                    // Now add the prop toggles
                    for (int i = 0; i < numItemsThisPage && queue.Count > 0; i++)
                    {
                        var prop = queue.Dequeue();
                        currentPage.controls.Add(new VRCExpressionsMenu.Control()
                        {
                            name = prop.Name,
                            type = VRCExpressionsMenu.Control.ControlType.Toggle,
                            icon = prop.Icon,
                            parameter = new VRCExpressionsMenu.Control.Parameter() { name = $"{Prefix}{prop.Name}_Toggle" }
                        });
                    }

                    // If we need a previous button (not first page and has previous page)
                    if (page > 0 && previousPage != null)
                    {
                        // Add a previous button
                        currentPage.controls.Add(new VRCExpressionsMenu.Control()
                        {
                            name = "Previous",
                            type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = previousPage
                        });
                    }

                    // Prepare next loop iteration
                    previousPage = currentPage;
                    currentPage = nextPage;
                    nextPage = null;
                    page++;
                }
                while (queue.Count > 0);

                // Add the first page
                menu.controls.Add(new VRCExpressionsMenu.Control()
                {
                    name = "QTProps",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = QTHelpers.CreateOrGetVRCMenuAsset(my.Avatar, $"QTPropsMenu0")
                });
            }
        }
    }
}
#endif