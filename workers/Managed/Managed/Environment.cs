using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Improbable.Collections;
using Improbable.Worker;
using Improbable;

namespace Managed
{
    public class Environment
    {
        public const int GridSize = 4;

        public struct GridPosition
        {
            private readonly int x;
            private readonly int z;

            public GridPosition(double x, double z)
            {
                this.x = ToGridIndex(x);
                this.z = ToGridIndex(z);
            }

            public GridPosition(Coordinates Coordinates) : this(Coordinates.x, Coordinates.z)
            {
            }

            public static int ToGridIndex(double x)
            {
                return Convert.ToInt32(Math.Floor(x / GridSize));
            }

            public bool Contains(Coordinates Coordinates)
            {
                return Contains(Coordinates.x, Coordinates.z);
            }

            private bool Contains(double x, double z)
            {
                return this.x == ToGridIndex(x) && this.z == ToGridIndex(z);
            }

            public override string ToString()
            {
                return String.Format("({0}, {1})", x, z);
            }
        }

        public enum MarkerType
        {
            Food,
            Nest
        };

        public readonly ComponentMap<Position> Positions;
       

        private readonly Connection Connection;

       

        public static CommandParameters AllowShortCircuiting = new CommandParameters();

        public Environment(Dispatcher Dispatcher, Connection Connection)
        {
            this.Connection = Connection;
            Positions = new ComponentMap<Position>(Dispatcher);
          
        }

        public void Delete(EntityId id)
        {
            Connection.SendDeleteEntityRequest(id, new Option<uint>());
        }

        
        

        

        public bool ChangedGridPos(double x0, double z0, double x1, double z1)
        {
            return GridPosition.ToGridIndex(x0) != GridPosition.ToGridIndex(x1) ||
                   GridPosition.ToGridIndex(z0) != GridPosition.ToGridIndex(z1);
        }

        private void Log(LogLevel level, String loggerName, String message)
        {
            Connection.SendLogMessage(level, loggerName, message);
        }

        public void LogError(String loggerName, String message)
        {
            Log(LogLevel.Error, loggerName, message);
        }

        public void LogWarning(String loggerName, String message)
        {
            Log(LogLevel.Warn, loggerName, message);
        }

        public void LogInfo(String loggerName, String message)
        {
            Log(LogLevel.Info, loggerName, message);
        }
    }
}

