#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Phasebreak.Editor
{
    // Samples Generic source transforms into muscle curves on a temporary avatar. The source
    // importer stays Generic: importing its animation tracks as Humanoid asserts in Unity 6.
    public static class BuildDirectionalLocomotion
    {
        private const string Source = "Assets/Phasebreak/Art/KayKitAdventurers/Characters/Knight.fbx";
        private const string Output = "Assets/Phasebreak/Animations/Player";
        private const string ControllerPath = "Assets/SourceFiles/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
        private static readonly (string bone, HumanBodyBones human)[] Map =
        {
            ("hips",HumanBodyBones.Hips),("spine",HumanBodyBones.Spine),("chest",HumanBodyBones.Chest),
            ("head",HumanBodyBones.Head),
            ("upperarm.l",HumanBodyBones.LeftUpperArm),("lowerarm.l",HumanBodyBones.LeftLowerArm),("hand.l",HumanBodyBones.LeftHand),
            ("upperarm.r",HumanBodyBones.RightUpperArm),("lowerarm.r",HumanBodyBones.RightLowerArm),("hand.r",HumanBodyBones.RightHand),
            ("upperleg.l",HumanBodyBones.LeftUpperLeg),("lowerleg.l",HumanBodyBones.LeftLowerLeg),("foot.l",HumanBodyBones.LeftFoot),("toes.l",HumanBodyBones.LeftToes),
            ("upperleg.r",HumanBodyBones.RightUpperLeg),("lowerleg.r",HumanBodyBones.RightLowerLeg),("foot.r",HumanBodyBones.RightFoot),("toes.r",HumanBodyBones.RightToes)
        };

        [MenuItem("Phasebreak/Author Directional Player Locomotion")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring locomotion.");
            EnsureFolder(Output);
            var clips = AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().ToDictionary(c=>c.name);
            GameObject source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
            source.hideFlags = HideFlags.HideAndDontSave;
            Avatar avatar = null;
            try
            {
                source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                source.transform.localScale = Vector3.one;
                var animator = source.GetComponent<Animator>();
                animator.enabled = false;
                clips["T-Pose"].SampleAnimation(source, 0f);
                var description = new HumanDescription
                {
                    human = Map.Select(m => new HumanBone { boneName=m.bone, humanName=HumanTrait.BoneName[(int)m.human], limit=new HumanLimit { useDefaultValues=true } }).ToArray(),
                    skeleton = source.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone {name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
                    upperArmTwist=.5f, lowerArmTwist=.5f, upperLegTwist=.5f, lowerLegTwist=.5f,
                    armStretch=.05f, legStretch=.05f, feetSpacing=0f, hasTranslationDoF=false
                };
                avatar = AvatarBuilder.BuildHumanAvatar(source, description);
                if (!avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("Temporary KayKit avatar is invalid.");
                using var handler = new HumanPoseHandler(avatar, source.transform);
                foreach (string name in new[]{"Running_A","Walking_Backwards","Running_Strafe_Left","Running_Strafe_Right","Jump_Start","Jump_Idle","Jump_Land"})
                    Bake(source, handler, clips[name], !name.StartsWith("Jump_") || name=="Jump_Idle");
                BuildController();
                AssetDatabase.SaveAssets();
                Debug.Log("PHASEBREAK_DIRECTIONAL_LOCOMOTION: baked seven clips; source remains Generic; controller updated.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                if (avatar != null) UnityEngine.Object.DestroyImmediate(avatar);
            }
        }

        private static void Bake(GameObject source, HumanPoseHandler handler, AnimationClip original, bool loop)
        {
            string path=$"{Output}/{original.name}.anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip==null) { clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,path); }
            clip.ClearCurves(); clip.name=original.name; clip.frameRate=60f;
            var curves=new AnimationCurve[HumanTrait.MuscleCount+7];
            for(int i=0;i<curves.Length;i++) curves[i]=new AnimationCurve();
            var pose=new HumanPose();
            int frames=Mathf.CeilToInt(original.length*60f);
            Quaternion previous=Quaternion.identity;
            for(int frame=0;frame<=frames;frame++)
            {
                float time=original.length*frame/frames;
                original.SampleAnimation(source,time);
                handler.GetHumanPose(ref pose);
                for(int i=0;i<HumanTrait.MuscleCount;i++) curves[i].AddKey(time,pose.muscles[i]);
                Quaternion rotation=pose.bodyRotation;
                if(frame>0 && Quaternion.Dot(previous,rotation)<0) rotation=new Quaternion(-rotation.x,-rotation.y,-rotation.z,-rotation.w);
                previous=rotation;
                float[] body={pose.bodyPosition.x,pose.bodyPosition.y,pose.bodyPosition.z,rotation.x,rotation.y,rotation.z,rotation.w};
                for(int i=0;i<7;i++) curves[HumanTrait.MuscleCount+i].AddKey(time,body[i]);
            }
            for(int i=0;i<curves.Length;i++)
            {
                // Expand the short-legged source's travel arcs on the taller target while
                // retaining the authored timing and knee bends. Ground fitting below then
                // adjusts body height to the target's support foot.
                if(loop && original.name!="Jump_Idle" && i<HumanTrait.MuscleCount &&
                    HumanTrait.MuscleName[i].Contains("Upper Leg"))
                {
                    float gain=HumanTrait.MuscleName[i].EndsWith("In-Out") ? 1.8f :
                        HumanTrait.MuscleName[i].EndsWith("Front-Back") ? 1.6f : 1f;
                    var keys=curves[i].keys; float center=keys.Average(k=>k.value);
                    for(int k=0;k<keys.Length;k++) keys[k].value=Mathf.Clamp(center+(keys[k].value-center)*gain,-1f,1f);
                    curves[i].keys=keys;
                }
                var sampled=curves[i].keys;
                if(sampled.All(k=>Mathf.Abs(k.value-sampled[0].value)<.00001f))
                    curves[i]=AnimationCurve.Linear(0f,sampled[0].value,original.length,sampled[0].value);
                // Linear sampled tangents prevent overshoot at planted feet and loop seams.
                for(int k=0;k<curves[i].length;k++)
                { AnimationUtility.SetKeyLeftTangentMode(curves[i],k,AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curves[i],k,AnimationUtility.TangentMode.Linear); }
                string property=i<HumanTrait.MuscleCount ? HumanTrait.MuscleName[i] : new[]{"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"}[i-HumanTrait.MuscleCount];
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),property),curves[i]);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime=loop; settings.loopBlend=loop;
            settings.keepOriginalOrientation=true; settings.keepOriginalPositionXZ=true; settings.keepOriginalPositionY=true;
            settings.loopBlendOrientation=true; settings.loopBlendPositionXZ=true; settings.loopBlendPositionY=true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            FitGroundContact(clip, loop && original.name != "Jump_Idle");
            EditorUtility.SetDirty(clip);
        }

        private static void FitGroundContact(AnimationClip clip, bool locomotion)
        {
            // KayKit has very short legs compared with RegularMale. Muscle retargeting alone
            // preserves its body bob but leaves both target feet floating. Bake the target's
            // support-foot height; this is a visual body curve, never gameplay displacement.
            GameObject target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Phasebreak/Prefabs/Player/RegularMaleVisual.prefab"));
            target.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                var animator=target.GetComponentInChildren<Animator>();
                animator.enabled=false;
                var idle=animator.runtimeAnimatorController.animationClips.First(c=>c.name=="Idle");
                idle.SampleAnimation(animator.gameObject,0f);
                Transform left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                float ground=Mathf.Min(left.position.y,right.position.y);
                var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),"RootT.y");
                var y=AnimationUtility.GetEditorCurve(clip,binding);
                var corrected=new AnimationCurve();
                int frames=Mathf.CeilToInt(clip.length*60f);
                for(int frame=0;frame<=frames;frame++)
                {
                    float time=clip.length*frame/frames;
                    clip.SampleAnimation(animator.gameObject,time);
                    float offset=locomotion ? ground-Mathf.Min(left.position.y,right.position.y) : 0f;
                    corrected.AddKey(time,y.Evaluate(time)+offset/animator.humanScale);
                }
                if(locomotion)
                {
                    for(int i=0;i<corrected.length;i++) corrected.SmoothTangents(i,0f);
                    AnimationUtility.SetEditorCurve(clip,binding,corrected);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(target); }
        }

        private static void BuildController()
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            foreach(string parameter in new[]{"MoveX","MoveZ"})
                if(!controller.parameters.Any(p=>p.name==parameter)) controller.AddParameter(parameter,AnimatorControllerParameterType.Float);
            var machine=controller.layers[0].stateMachine;
            var move=machine.states.Single(s=>s.state.name=="Idle Walk Run Blend").state;
            // Reuse the existing subasset, preserving references and rerun idempotence.
            var tree=(BlendTree)move.motion;
            var idle=controller.animationClips.First(c=>c.name=="Idle");
            tree.name="Directional Locomotion"; tree.blendType=BlendTreeType.SimpleDirectional2D;
            tree.blendParameter="MoveX"; tree.blendParameterY="MoveZ"; tree.useAutomaticThresholds=false;
            tree.children=new[]
            {
                Child(idle,Vector2.zero), Child(Load("Running_A"),Vector2.up),
                Child(Load("Walking_Backwards"),Vector2.down, 4f/3f, .5333f),
                Child(Load("Running_Strafe_Left"),Vector2.left), Child(Load("Running_Strafe_Right"),Vector2.right)
            };
            move.speedParameter="MotionSpeed"; move.speedParameterActive=true;
            var takeoff=machine.states.Single(s=>s.state.name=="JumpStart").state;
            var air=machine.states.Single(s=>s.state.name=="InAir").state;
            var land=machine.states.Single(s=>s.state.name=="JumpLand").state;
            takeoff.motion=Load("Jump_Start"); takeoff.speed=2.5f;
            air.motion=Load("Jump_Idle"); air.speed=1f;
            var oldLandTree=land.motion as BlendTree;
            land.motion=Load("Jump_Land"); land.speed=2.5f;
            if(oldLandTree!=null) UnityEngine.Object.DestroyImmediate(oldLandTree,true);
            takeoff.speedParameterActive=air.speedParameterActive=land.speedParameterActive=false;
            foreach(var s in new[]{move,takeoff,air,land})
            {
                // Muscle-only baked clips have no LeftFoot/RightFoot IK goal curves.
                // Inherited Starter Assets foot IK would pull both feet to its default goal.
                s.iKOnFeet=false;
                foreach(var t in s.transitions.ToArray()) { s.RemoveTransition(t); UnityEngine.Object.DestroyImmediate(t,true); }
            }
            Transition(move,takeoff,"Jump",true,.06f);
            Transition(move,air,"FreeFall",true,.08f);
            Transition(takeoff,land,"Grounded",true,.05f);
            var airborne=takeoff.AddTransition(air); airborne.hasExitTime=true; airborne.exitTime=.85f; airborne.duration=.06f; airborne.hasFixedDuration=true;
            Transition(air,land,"Grounded",true,.05f);
            Transition(land,takeoff,"Jump",true,.04f);
            Transition(land,air,"FreeFall",true,.05f);
            var moving=Transition(land,move,"Grounded",true,.09f); moving.AddCondition(AnimatorConditionMode.Greater,.1f,"Speed");
            var settled=land.AddTransition(move); settled.hasExitTime=true; settled.exitTime=.8f; settled.duration=.1f; settled.hasFixedDuration=true;
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(tree);
        }
        private static AnimatorStateTransition Transition(AnimatorState from,AnimatorState to,string parameter,bool value,float duration)
        {
            var t=from.AddTransition(to); t.hasExitTime=false; t.hasFixedDuration=true; t.duration=duration;
            t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,0,parameter); return t;
        }
        private static ChildMotion Child(Motion motion,Vector2 position,float scale=1f,float phase=0f) => new ChildMotion {motion=motion,position=position,timeScale=scale,cycleOffset=phase};
        private static AnimationClip Load(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Output}/{name}.anim");
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            int i=path.LastIndexOf('/'); EnsureFolder(path.Substring(0,i)); AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));
        }
    }
}
#endif
