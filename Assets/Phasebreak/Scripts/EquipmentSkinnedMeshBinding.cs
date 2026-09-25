using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [Serializable]
    public struct EquipmentSkinBone
    {
        public int meshBoneIndex;
        public HumanBodyBones playerBone;

        public EquipmentSkinBone(int meshBoneIndex, HumanBodyBones playerBone)
        {
            this.meshBoneIndex = meshBoneIndex;
            this.playerBone = playerBone;
        }
    }

    // The authored mesh bindposes already target the player's resting rig. This only supplies
    // the live animated transforms; no second animator or equipment state is created.
    [DisallowMultipleComponent]
    public sealed class EquipmentSkinnedMeshBinding : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer skin;
        [SerializeField] private EquipmentSkinBone[] boneMap = Array.Empty<EquipmentSkinBone>();
        [SerializeField] private HumanBodyBones rootBone = HumanBodyBones.Hips;

        public void Configure(SkinnedMeshRenderer renderer, EquipmentSkinBone[] bindings,
            HumanBodyBones root = HumanBodyBones.Hips)
        {
            skin = renderer;
            boneMap = bindings ?? Array.Empty<EquipmentSkinBone>();
            rootBone = root;
        }

        public bool Bind(Animator playerAnimator)
        {
            if (skin == null || skin.sharedMesh == null || playerAnimator == null || !playerAnimator.isHuman)
            {
                Debug.LogWarning("Equipment skin could not bind to the player humanoid rig.", this);
                return false;
            }

            Transform[] bones = new Transform[skin.sharedMesh.bindposes.Length];
            Transform root = playerAnimator.GetBoneTransform(rootBone);
            if (root == null) return false;
            Array.Fill(bones, root); // Unweighted source bones have no effect on the rendered mesh.
            foreach (EquipmentSkinBone entry in boneMap)
            {
                if (entry.meshBoneIndex < 0 || entry.meshBoneIndex >= bones.Length)
                    return false;
                Transform target = playerAnimator.GetBoneTransform(entry.playerBone);
                if (target == null) return false;
                bones[entry.meshBoneIndex] = target;
            }
            skin.bones = bones;
            skin.rootBone = root;
            return true;
        }
    }
}
