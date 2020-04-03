using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Worker;
using Environment = Managed.Environment;

namespace src
{
    public interface StaticGrid
    {
        bool Contains(Environment.GridPosition GridPosition);
        bool TryGetValue(double x, double z, out EntityId EntityId);
    }

    public class StaticItemGrid<MetaClass> : StaticGrid where MetaClass : IComponentMetaclass
    {
        private readonly Dictionary<Environment.GridPosition, EntityId> Grid =
            new Dictionary<Environment.GridPosition, EntityId>();

        private readonly ComponentMap<Position> Positions;
        private readonly ComponentMap<MetaClass> Items;
        private readonly Connection Connection;

        private readonly HashSet<Environment.GridPosition> ReservedGrids = new HashSet<Environment.GridPosition>();
        private readonly HashSet<EntityId> PositionUnresolvedEntityIds = new HashSet<EntityId>();

        public StaticItemGrid(Dispatcher Dispatcher, Connection Connection, ComponentMap<Position> allPositions, ComponentMap<MetaClass> Items)
        {
            Dispatcher.OnAddComponent<MetaClass>(OnAddMetaClassComponent);
            Dispatcher.OnAddComponent<Position>(OnAddPosition);
            Dispatcher.OnRemoveEntity(RemoveItem);
            Positions = allPositions;
            this.Items = Items;
            this.Connection = Connection;
        }

        private void OnAddPosition(AddComponentOp<Position> Position)
        {
            if (PositionUnresolvedEntityIds.Contains(Position.EntityId))
            {
                PositionUnresolvedEntityIds.Remove(Position.EntityId);
                AddItem(Position.EntityId, Position.Data.Get().Value);
            }
        }

        private void OnAddMetaClassComponent(AddComponentOp<MetaClass> Component)
        {
            EntityId EntityId = Component.EntityId;
            IComponentData<Position> Position;
            if (!Positions.TryGetValue(EntityId, out Position))
            {
                PositionUnresolvedEntityIds.Add(EntityId);
            }
            else
            {
                AddItem(EntityId, Position.Get().Value);
            }
        }

        private void AddItem(EntityId EntityId, PositionData Position)
        {
            var position = Position.coords;
            var gridPosition = new Environment.GridPosition(position.x, position.z);
            if (!Grid.ContainsKey(gridPosition))
            {
                Grid.Add(gridPosition, EntityId);
                if (ReservedGrids.Contains(gridPosition))
                {
                    ReservedGrids.Remove(gridPosition);
                }
            }
            else
            {
                Connection.SendLogMessage(LogLevel.Warn, String.Format("StatigGrid<{0}>", typeof(MetaClass)),
                    String.Format("Duplicate item of type {1} at {0}", gridPosition, typeof(MetaClass)));
            }
        }

        private void RemoveItem(RemoveEntityOp RemoveEntityOp)
        {
            if (Items.ContainsKey(RemoveEntityOp.EntityId))
            {
                var position = Positions.Get(RemoveEntityOp.EntityId).Get().Value.coords;
                Grid.Remove(new Environment.GridPosition(position.x, position.z));
            }
            if (PositionUnresolvedEntityIds.Contains(RemoveEntityOp.EntityId))
            {
                PositionUnresolvedEntityIds.Remove(RemoveEntityOp.EntityId);
            }
        }


        public bool Contains(Environment.GridPosition GridPosition)
        {
            return Grid.ContainsKey(GridPosition);
        }

        public bool Contains(double x, double z)
        {
            return Contains(new Environment.GridPosition(x, z));
        }

        public bool TryGetValue(Environment.GridPosition GridPosition, out EntityId EntityId)
        {
            return Grid.TryGetValue(GridPosition, out EntityId);
        }


        public bool TryGetValue(double x, double z, out EntityId EntityId)
        {
            return TryGetValue(new Environment.GridPosition(x, z), out EntityId);
        }

        public bool IsReserved(double x, double z)
        {
            return ReservedGrids.Contains(new Environment.GridPosition(x, z));
        }


        public void Reserve(double x, double z)
        {
            ReservedGrids.Add(new Environment.GridPosition(x, z));
        }
    }
}
