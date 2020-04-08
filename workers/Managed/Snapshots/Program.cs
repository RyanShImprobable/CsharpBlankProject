using System;
using System.Collections.Generic;
using System.Linq;
using Improbable.Worker;

namespace Snapshots
{
    class Snapshots
    {
        private static long id = 1L;
        private static EntityId NextId
        {
            get { return new EntityId(id++); }
        }

        static void Main(string[] args)
        {

        }

        private static void GenerateBasic(string path, int numAnts, int WorldSize, int FoodClumps)
        {
            IDictionary<EntityId, Entity> entities = new Dictionary<EntityId, Entity>();

        }
    }
}
