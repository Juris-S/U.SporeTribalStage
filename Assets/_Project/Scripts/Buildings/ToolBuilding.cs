using SporeTribal.Definitions;
using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Buildings
{
    public class ToolBuilding : Building
    {
        [SerializeField] private ToolDefinition tool;

        public ToolDefinition Tool => tool;

        public static ToolBuilding Find(Village owner, ToolDefinition tool)
        {
            if (tool == null) return null;
            foreach (Building building in All)
                if (building is ToolBuilding tools && tools.Owner == owner && tools.Tool == tool && tools.IsStanding) return tools;
            return null;
        }
    }
}
