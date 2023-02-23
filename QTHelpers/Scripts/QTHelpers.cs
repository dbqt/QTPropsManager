#if UNITY_EDITOR
using AnimatorAsCode.V0;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace QTAssets
{
    public static class QTHelpers
    {
        /// <summary>
        /// Checks if the base folder for the avatar exist, if not, creates i, then returns it.
        /// </summary>
        public static string EnsureBaseFolderExists(VRCAvatarDescriptor avatarDescriptor)
        {
            // Create base folders if doesn't exist
            if (!System.IO.Directory.Exists("Assets/QTAssets/QTProps/Generated/"))
            {
                System.IO.Directory.CreateDirectory("Assets/QTAssets/QTProps/Generated/");
                AssetDatabase.Refresh();
            }

            // Create unique named folder if doesn't exist
            string safeName = string.Join("", avatarDescriptor.name.Split('?', '<', '>', '\\', ':', '*', '|', '\"', '/'));
            if (!AssetDatabase.IsValidFolder($"Assets/QTAssets/QTProps/Generated/{safeName}"))
            {
                string uniqueGuid = AssetDatabase.CreateFolder("Assets/QTAssets/QTProps/Generated", safeName);
                AssetDatabase.Refresh();
            }

            return $"Assets/QTAssets/QTProps/Generated/{safeName}/";
        }

        /// <summary>
        /// Checks if the file at the specified path exist.
        /// </summary>
        public static bool CheckAssetExists(string path)
        {
            return System.IO.File.Exists(path);
        }

        /// <summary>
        /// Generates an Animator As Code base with some default setup.
        /// </summary>
        public static AacFlBase AnimatorAsCode(string systemName, VRCAvatarDescriptor avatar, AnimatorController assetContainer, string assetKey, bool useWriteDefaults)
        {
            var aac = AacV0.Create(new AacConfiguration
            {
                SystemName = systemName,
                // In the examples, we consider the avatar to be also the animator root.
                AvatarDescriptor = avatar,
                // You can set the animator root to be different than the avatar descriptor,
                // if you want to apply an animator to a different avatar without redefining
                // all of the game object references which were relative to the original avatar.
                AnimatorRoot = avatar.transform,
                // DefaultValueRoot is currently unused in AAC. It is added here preemptively
                // in order to define an avatar root to sample default values from.
                // The intent is to allow animators to be created with Write Defaults OFF,
                // but mimicking the behaviour of Write Defaults ON by automatically
                // sampling the default value from the scene relative to the transform
                // defined in DefaultValueRoot.
                DefaultValueRoot = avatar.transform,
                AssetContainer = assetContainer,
                AssetKey = assetKey,
                DefaultsProvider = new AacDefaultsProvider(writeDefaults: useWriteDefaults)
            });
            aac.ClearPreviousAssets();
            return aac;
        }

        /// <summary>
        /// Grabs the animator controller of the specified type from the avatar descriptor.
        /// </summary>
        public static AnimatorController GetAnimatorController(VRCAvatarDescriptor avatarDescriptor, VRCAvatarDescriptor.AnimLayerType animationLayerType)
        {
            return avatarDescriptor.baseAnimationLayers.FirstOrDefault(layer => layer.type == animationLayerType).animatorController as AnimatorController;
        } 

        /// <summary>
        /// Finds and deletes all layers that start with the specified name from the animator controller.
        /// </summary>
        public static void RemoveAnimatorLayersByName(AnimatorController controller, string name)
        {
            var toKeep = controller.layers.Where(layer => !layer.name.StartsWith(name));
            controller.layers = toKeep.ToArray();
        }

        /// <summary>
        /// Finds and deletes all the parameters that start with the specified name from the animator controller.
        /// </summary>
        public static void RemoveParametersFromAnimatorByName(AnimatorController controller, string name)
        {
            for (int i = controller.parameters.Length - 1; i >= 0; i--)
            {
                if (controller.parameters[i].name.StartsWith(name))
                {
                    controller.RemoveParameter(i);
                }
            }
        }

        /// <summary>
        /// Removes the parameter from the animator controller.
        /// </summary>
        public static void RemoveParameterFromAnimator(AnimatorController controller, string name)
        {
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name.Equals(name))
                {
                    controller.RemoveParameter(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Ensures that an animator controller exist on the avatar on the FX layer, if not, generate one.
        /// </summary>
        public static void EnsureFXAnimatorExists(VRCAvatarDescriptor avatarDescriptor)
        {
            if (avatarDescriptor.baseAnimationLayers.FirstOrDefault(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX).animatorController == null)
            {
                string folder = EnsureBaseFolderExists(avatarDescriptor);

                // Create animator if doesn't exist
                AnimatorController animator;
                if (!System.IO.File.Exists($"{folder}FX.controller"))
                {
                    AssetDatabase.CopyAsset("Assets/QTAssets/QTHelpers/Resources/EmptyFX.controller", $"{folder}FX.controller");
                    AssetDatabase.SaveAssets();
                }
                animator = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{folder}FX.controller");
                AssetDatabase.Refresh();

                // Assign animator
                avatarDescriptor.customizeAnimationLayers = true;
                avatarDescriptor.baseAnimationLayers[4].isDefault = false;
                avatarDescriptor.baseAnimationLayers[4].animatorController = animator;
            }
        }

        /// <summary>
        /// Ensure the avatar has custom VRC menu and parameters, if not, generate them.
        /// </summary>
        public static void EnsureVRCMenuAndParametersExist(VRCAvatarDescriptor avatarDescriptor)
        {
            avatarDescriptor.customExpressions = true;
            if (avatarDescriptor.expressionsMenu == null)
            {
                avatarDescriptor.expressionsMenu = CreateOrGetVRCMenuAsset(avatarDescriptor, "Menu");
            }

            if (avatarDescriptor.expressionParameters == null)
            {
                avatarDescriptor.expressionParameters = CreateOrGetVRCParametersAsset(avatarDescriptor, "Parameters");
            }
        }

        /// <summary>
        /// Ensure the avatar has asset container asset, if not, generate them.
        /// </summary>
        public static AnimatorController EnsureAssetContainerExist(VRCAvatarDescriptor avatarDescriptor)
        {
            string folder = EnsureBaseFolderExists(avatarDescriptor);
            // Create animator if doesn't exist
            AnimatorController animator;
            if (!System.IO.File.Exists($"{folder}AssetContainer.controller"))
            {
                AssetDatabase.CopyAsset("Assets/QTAssets/QTHelpers/Resources/EmptyFX.controller", $"{folder}AssetContainer.controller");
                AssetDatabase.SaveAssets();
            }
            animator = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{folder}AssetContainer.controller");
            AssetDatabase.Refresh();

            return animator;
        }

        /// <summary>
        /// Delete the asset container associated with the avatar descriptor.
        /// </summary>
        public static void DeleteAssetContainer(VRCAvatarDescriptor avatarDescriptor)
        {
            string folder = EnsureBaseFolderExists(avatarDescriptor);
            if (System.IO.File.Exists($"{folder}AssetContainer.controller"))
            {
                AssetDatabase.DeleteAsset($"{folder}AssetContainer.controller");
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Gets or creates the VRC Menu Asset using the provided name for the specified avatar.
        /// </summary>
        public static VRCExpressionsMenu CreateOrGetVRCMenuAsset(VRCAvatarDescriptor avatarDescriptor, string name)
        {
            string folder = EnsureBaseFolderExists(avatarDescriptor);

            VRCExpressionsMenu menu;
            if (!System.IO.File.Exists($"{folder}{name}.asset"))
            {
                menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>(); //new VRCExpressionsMenu();
                AssetDatabase.CreateAsset(menu, $"{folder}{name}.asset");
                AssetDatabase.SaveAssets();
            }
            else
            {
                menu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>($"{folder}{name}.asset");
            }
            AssetDatabase.Refresh();

            return menu;
        }

        /// <summary>
        /// Deletes any VRC Menu assets that includes the search terms from the specified avatar.
        /// </summary>
        public static void DeleteVRCMenuAssets(VRCAvatarDescriptor avatarDescriptor, string searchTerms)
        {
            string folder = EnsureBaseFolderExists(avatarDescriptor);
            var guids = AssetDatabase.FindAssets($"{searchTerms}", new string[] { folder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                DeleteAsset(path);
            }
        }

        /// <summary>
        /// Deletes the asset at the path if it exists.
        /// </summary>
        public static void DeleteAsset(string path)
        {
            if (System.IO.File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Gets or creates the VRC Parameters asset with the specified name from the specified avatar.
        /// </summary>
        public static VRCExpressionParameters CreateOrGetVRCParametersAsset(VRCAvatarDescriptor avatarDescriptor, string name)
        {
            string folder = EnsureBaseFolderExists(avatarDescriptor);

            VRCExpressionParameters parameters;
            if (!System.IO.File.Exists($"{folder}{name}.asset"))
            {
                parameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                parameters.parameters = new VRCExpressionParameters.Parameter[0];

                AssetDatabase.CreateAsset(parameters, $"{folder}{name}.asset");
                AssetDatabase.SaveAssets();
            }
            else
            {
                parameters = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>($"{folder}{name}.asset");
            }
            AssetDatabase.Refresh();

            return parameters;
        }

        /// <summary>
        /// Adds (if it doesn't exist) the bool VRC Parameter to the avatar.
        /// </summary>
        public static void AddVRCParameter(VRCAvatarDescriptor avatarDescriptor, string parameter, bool defaultValue = false, bool saved = false)
        {
            var vrcParamList = new List<VRCExpressionParameters.Parameter>(avatarDescriptor.expressionParameters.parameters);
            if (avatarDescriptor.expressionParameters.FindParameter(parameter) == null)
            {
                vrcParamList.Add(new VRCExpressionParameters.Parameter() { name = parameter, valueType = VRCExpressionParameters.ValueType.Bool, defaultValue = defaultValue ? 1 : 0, saved = saved });
            }
            avatarDescriptor.expressionParameters.parameters = vrcParamList.ToArray();
        }

        /// <summary>
        /// Adds (if it doesn't exist) the int VRC Parameter to the avatar.
        /// </summary>
        public static void AddVRCParameter(VRCAvatarDescriptor avatarDescriptor, string parameter, int defaultValue, bool saved)
        {
            var vrcParamList = new List<VRCExpressionParameters.Parameter>(avatarDescriptor.expressionParameters.parameters);
            if (avatarDescriptor.expressionParameters.FindParameter(parameter) == null)
            {
                vrcParamList.Add(new VRCExpressionParameters.Parameter() { name = parameter, valueType = VRCExpressionParameters.ValueType.Int, defaultValue = defaultValue, saved = saved });
            }
            avatarDescriptor.expressionParameters.parameters = vrcParamList.ToArray();
        }

        /// <summary>
        /// Adds (if it doesn't exist) the float VRC Parameter to the avatar.
        /// </summary>
        public static void AddVRCParameter(VRCAvatarDescriptor avatarDescriptor, string parameter, float defaultValue, bool saved)
        {
            var vrcParamList = new List<VRCExpressionParameters.Parameter>(avatarDescriptor.expressionParameters.parameters);
            if (avatarDescriptor.expressionParameters.FindParameter(parameter) == null)
            {
                vrcParamList.Add(new VRCExpressionParameters.Parameter() { name = parameter, valueType = VRCExpressionParameters.ValueType.Int, defaultValue = defaultValue, saved = saved });
            }
            avatarDescriptor.expressionParameters.parameters = vrcParamList.ToArray();
        }

        /// <summary>
        /// Removes the specified VRC parameter from avatar if it exists.
        /// </summary>
        public static void RemoveVRCParameter(VRCAvatarDescriptor avatarDescriptor, string parameter)
        {
            var vrcParamList = new List<VRCExpressionParameters.Parameter>(avatarDescriptor.expressionParameters.parameters);
            var param = avatarDescriptor.expressionParameters.FindParameter(parameter);
            if (param != null)
            {
                vrcParamList.Remove(param);
            }
            avatarDescriptor.expressionParameters.parameters = vrcParamList.ToArray();
        }

        /// <summary>
        /// Removes all VRC parameters that contains the specified name from avatar if any exists.
        /// </summary>
        public static void RemoveVRCParametersByName(VRCAvatarDescriptor avatarDescriptor, string name)
        {
            var vrcParamList = new List<VRCExpressionParameters.Parameter>(avatarDescriptor.expressionParameters.parameters);

            var paramsToDelete = vrcParamList.Where(p => p.name.Contains(name)).ToList();

            foreach (var param in paramsToDelete)
            {
                vrcParamList.Remove(param);
            }
            avatarDescriptor.expressionParameters.parameters = vrcParamList.ToArray();
        }

        /// <summary>
        /// Looks at children for an object with the name. Only checks immediate children.
        /// </summary>
        public static Transform GetChildByName(this Transform parent, string name)
        {
            Transform result = null;

            if (parent != null)
            {
                var current = parent;
                for(int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (child.name.Equals(name))
                    {
                        result = child;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a list of immediate children of the transform.
        /// </summary>
        public static List<Transform> GetChildren(this Transform parent)
        {
            List<Transform> result = new List<Transform>();
            for(int i = 0; i < parent.childCount; i++)
            {
                result.Add(parent.GetChild(i));
            }
            return result;
        }
    }
}
#endif