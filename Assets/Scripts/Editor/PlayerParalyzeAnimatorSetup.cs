#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 一次性為水中 Player.controller 加入麻痺狀態（選單執行）。
/// </summary>
public static class PlayerParalyzeAnimatorSetup
{
    const string ControllerPath = "Assets/Scenes/動畫/水中/Player.controller";
    const string ClipPath = "Assets/Scenes/動畫/水中/paralyze.anim";
    const string BoolParam = "IsParalyzed";
    const string TriggerParam = "Paralyze";
    const string StateName = "Paralyze";
    const string ReturnStateName = "SwimTree";

    [MenuItem("Tools/深海之下/設定玩家麻痺 Animator（水中）")]
    public static void SetupUnderwaterPlayer()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (controller == null)
        {
            Debug.LogError("[PlayerParalyzeSetup] 找不到 " + ControllerPath);
            return;
        }
        if (clip == null)
        {
            Debug.LogError("[PlayerParalyzeSetup] 找不到 " + ClipPath);
            return;
        }

        EnsureParameter(controller, BoolParam, AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, TriggerParam, AnimatorControllerParameterType.Trigger);

        var layer = controller.layers[0];
        var sm = layer.stateMachine;

        AnimatorState paralyzeState = FindState(sm, StateName);
        if (paralyzeState == null)
        {
            paralyzeState = sm.AddState(StateName, new Vector3(720, 50, 0));
            paralyzeState.motion = clip;
        }
        else
        {
            paralyzeState.motion = clip;
        }

        AnimatorState returnState = FindState(sm, ReturnStateName);
        if (returnState == null)
        {
            Debug.LogWarning("[PlayerParalyzeSetup] 找不到 " + ReturnStateName + "，麻痺結束將使用預設狀態。");
            returnState = sm.defaultState;
        }

        EnsureAnyStateTransition(sm, paralyzeState, BoolParam, true);
        EnsureExitTransition(paralyzeState, returnState, BoolParam, false);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerParalyzeSetup] 已完成：參數 " + BoolParam + " / " + TriggerParam + "，狀態 " + StateName);
    }

    static void EnsureParameter(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, type);
    }

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var child in sm.states)
            if (child.state != null && child.state.name == name)
                return child.state;
        return null;
    }

    static void EnsureAnyStateTransition(AnimatorStateMachine sm, AnimatorState dst, string param, bool whenTrue)
    {
        foreach (var t in sm.anyStateTransitions)
        {
            if (t.destinationState != dst) continue;
            if (t.conditions.Length == 1 && t.conditions[0].parameter == param)
                return;
        }

        var tr = sm.AddAnyStateTransition(dst);
        tr.AddCondition(whenTrue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        tr.duration = 0.05f;
        tr.hasExitTime = false;
        tr.canTransitionToSelf = false;
        tr.interruptionSource = TransitionInterruptionSource.None;
    }

    static void EnsureExitTransition(AnimatorState from, AnimatorState dst, string param, bool whenTrue)
    {
        if (dst == null) return;
        foreach (var t in from.transitions)
        {
            if (t.destinationState != dst) continue;
            if (t.conditions.Length == 1 && t.conditions[0].parameter == param)
                return;
        }

        var tr = from.AddTransition(dst);
        tr.AddCondition(whenTrue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        tr.duration = 0.1f;
        tr.hasExitTime = false;
    }
}
#endif
