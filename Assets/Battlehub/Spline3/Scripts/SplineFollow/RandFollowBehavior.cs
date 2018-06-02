using UnityEngine;

namespace Battlehub.Spline3
{
    public class RandFollowBehavior : SplineFollowBehavior
    {
        public bool AllowDirectionChange = false;

        protected override void OnFork(ForkEventArgs args)
        {
            int[] connections;
            if(AllowDirectionChange)
            {
                int connection = Random.Range(0, args.Junction.ConnectionsCount);
                args.SelectedConnectionIndex = connection;
            }
            else
            {
                if (SplineFollow.Speed >= 0)
                {
                    connections = args.Junction.GetOutputs();
                    if (connections.Length == 0)
                    {
                        connections = args.Junction.GetInputs();
                    }
                }
                else
                {
                    connections = args.Junction.GetInputs();
                    if (connections.Length == 0)
                    {
                        connections = args.Junction.GetOutputs();
                    }
                }

                if (connections.Length > 0)
                {
                    int connection = Random.Range(0, connections.Length);
                    args.SelectedConnectionIndex = connections[connection];
                }
            }
        }
    }

}
