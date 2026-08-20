using System.Collections.Generic;
using UnityEngine;

public abstract class AICondition : ScriptableObject
{
    public abstract bool CheckCondition(EnemyAI self, List<ITurnEntity> allCombatants, out CharacterStatus target);
}
