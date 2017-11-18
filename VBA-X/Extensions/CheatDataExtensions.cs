using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VBA_X
{
    static class CheatDataExtensions
    {
        public static IList<EmulatorComponent.CheatData> ConvertCheatData(this IList<Utility.CheatData> cheatData)
        {
            List<EmulatorComponent.CheatData> results = new List<EmulatorComponent.CheatData>(cheatData.Count);

            foreach (var cheat in cheatData)
            {
                EmulatorComponent.CheatData convertedItem = new EmulatorComponent.CheatData();
                convertedItem.Description = cheat.Description;
                convertedItem.CheatCode = cheat.CheatCode;
                convertedItem.Enabled = cheat.Enabled;
                results.Add(convertedItem);
            }

            return results;
        }
    }
}
