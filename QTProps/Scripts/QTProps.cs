#if UNITY_EDITOR
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEngine;
using UnityEditor.Animations;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;

namespace QTAssets
{
    public class QTProps : ScriptableObject
    {
        public AnimatorController AssetContainer;
        public VRCExpressionsMenu menuLocation;
        public bool UseWriteDefault;
        public List<QTProp> Props;

        public void SetupAssetContainer(VRCAvatarDescriptor avatar)
        {
            AssetContainer = QTHelpers.EnsureAssetContainerExist(avatar);
        }
    }
}
#endif
