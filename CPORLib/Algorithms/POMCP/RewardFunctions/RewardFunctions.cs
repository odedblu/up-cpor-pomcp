using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;

namespace CPORLib.Algorithms
{
    internal class RewardFunctions
    {
        public static double GeneralReward(State state, Problem p, Action preAction)
        {
            //if (state == null) return Double.MinValue;
            if (state != null && p.IsGoalState(state))
            {
                return 5.0;
            }
            else
            {
                if (state == null)
                {
                    return -100.0;
                }
            }
            return -1.0;
        }

  

    }
}
