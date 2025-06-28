using UdonSharp;
using UnityEngine;

public class ExperimentValidator : UdonSharpBehaviour
{
    public ReactionDictionary reactionDictionary;
    public ResultDisplayManager resultDisplay;
    public CompoundPrefabAssembler prefabAssembler;

    public void Validate(string[] elements, string environmentKey)
    {
        string key = reactionDictionary.GenerateReactionKey(elements, environmentKey);
        if (reactionDictionary.ContainsReaction(key))
        {
            string reactionName = reactionDictionary.GetReactionName(key);
            int style = reactionDictionary.GetVisualStyle(key);

            resultDisplay?.ShowResult($"{reactionName} が生成されました！");
            prefabAssembler?.GenerateCompound(reactionName, style);
        }
        else
        {
            resultDisplay?.ShowResult("反応に失敗しました。条件を見直してください。");
        }
    }
}
