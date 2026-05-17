using UnityEngine;

[CreateAssetMenu(fileName = "PreviewAnimationConfig", menuName = "Config/PreviewAnimationConfig")]
public class PreviewAnimationConfig : ScriptableObject
{
    public AnimationClip pistolIdle;
    public AnimationClip pistolWalk;
    public AnimationClip pistolRun;
    public AnimationClip pistolRunBackward;
}
