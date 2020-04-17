using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Improbable;
using Improbable.Worker;
using Improbable.Managed;
using Improbable.Collections;

namespace Managed
{
    class myTree
    {
        public myTree(int value)
        {
            burned = value;
        }
        public int burned;
    }
    internal class Startup
    {
        private static Dictionary<EntityId, myTree> trees = new Dictionary<EntityId, myTree>();
        private static System.Collections.Generic.List<EntityId> pending_entities = new System.Collections.Generic.List<EntityId>();
        private static readonly Random Random = new Random();

        private static long id = 1L;

        private const string WorkerType = "Managed";

        private const string LoggerName = "Startup.cs";

        private const int ErrorExitStatus = 1;

        private const uint GetOpListTimeoutInMilliseconds = 100;

        private static int flag_burned;

        private static readonly WorkerRequirementSet TreeWorker =
            new WorkerRequirementSet(new Improbable.Collections.List<WorkerAttributeSet>
            {
                new WorkerAttributeSet(new Improbable.Collections.List<string> {"simulation"})
            });

        private readonly Dictionary<EntityId, IComponentData<Tree>> Components =
            new Dictionary<EntityId, IComponentData<Tree>>();

        private static IDictionary<EntityId, Entity> entities = new Dictionary<EntityId, Entity>();

        private static EntityId NextId
        {
            get { return new EntityId(id++); }
        }

        private static int Main(string[] args)
        {
            if (args.Length != 4) {
                PrintUsage();
                return ErrorExitStatus;
            }

            // Avoid missing component errors because no components are directly used in this project
            // and the GeneratedCode assembly is not loaded but it should be
            Assembly.Load("GeneratedCode");

            var connectionParameters = new ConnectionParameters
            {
                WorkerType = WorkerType,
                Network =
                {
                    ConnectionType = NetworkConnectionType.Tcp
                }
            };

            using (var connection = ConnectWithReceptionist(args[1], Convert.ToUInt16(args[2]), args[3], connectionParameters))
            {
                var dispatcher = new Dispatcher();
                var isConnected = true;

                dispatcher.OnDisconnect(op =>
                {
                    Console.Error.WriteLine("[disconnect] " + op.Reason);
                    isConnected = false;
                });

                dispatcher.OnLogMessage(op =>
                {
                    connection.SendLogMessage(op.Level, LoggerName, op.Message);
                    if (op.Level == LogLevel.Fatal)
                    {
                        Console.Error.WriteLine("Fatal error: " + op.Message);
                        Environment.Exit(ErrorExitStatus);
                    }
                });

                dispatcher.OnAddEntity(op =>
                {
                    pending_entities.Add(op.EntityId);                  
                });

                dispatcher.OnAddComponent<Tree>(op =>
                {
                    if (pending_entities.Contains(op.EntityId))
                    {   
                        if (op.Data.Get().Value.burned == 0)
                        {
                            Tree.Update tupdate = new Tree.Update();
                            connection.SendComponentUpdate(op.EntityId, tupdate.SetBurned(flag_burned));
                            
                        }
                        trees.Add(op.EntityId, new myTree(op.Data.Get().Value.burned));

                    } else
                    {
                        throw new Exception();
                    }
                    
                });

                dispatcher.OnComponentUpdate<Tree>(op =>
                {
                    if (trees.ContainsKey(op.EntityId))
                    {
                        if (op.Update.Get().burned.HasValue)
                        {
                            trees[op.EntityId].burned = op.Update.Get().burned.Value;
                        }                     
                    }
                    else
                    {
                        throw new Exception();
                    }
                });

                dispatcher.OnFlagUpdate(op =>
                {
                    if (op.Name == "improbable_burned")
                    {
                        flag_burned = int.Parse(op.Value.Value);
                        foreach (KeyValuePair<EntityId, myTree> tree in trees)
                        {
                            Tree.Update tupdate = new Tree.Update();
                            connection.SendComponentUpdate(tree.Key, tupdate.SetBurned(flag_burned));
                        }
                    }
                });

                IDictionary<EntityId, Entity> entities = CreateTrees();
                foreach (KeyValuePair<EntityId, Entity> entry in entities)
                {
                    connection.SendCreateEntityRequest(entry.Value, entry.Key, new Option<uint>());
                }

                //Thread.Sleep(20000);

                String burned_value;
                connection.GetWorkerFlag("improbable_burned").TryGetValue(out burned_value);
                int burned_default = Int32.Parse(burned_value);
                foreach (KeyValuePair<EntityId, Entity> entry in entities)
                {
                    Tree.Update tupdate = new Tree.Update();
                    connection.SendComponentUpdate(entry.Key, tupdate.SetBurned(burned_default));
                }

                while (isConnected)
                {
                    using (var opList = connection.GetOpList(GetOpListTimeoutInMilliseconds))
                    {
                        dispatcher.Process(opList);
                    }
                }
            }

            // This means we forcefully disconnected
            return ErrorExitStatus;
        }

        public static IDictionary<EntityId, Entity> CreateTrees()
        {
            int WorldSize = 160;
            for (int i = 0; i < 500; i++)
            {
                double delta_x = Random.NextDouble() * 100 * (WorldSize / 100);
                double delta_z = Random.NextDouble() * 100 * (WorldSize / 100);
                double map_start = -WorldSize;
                double x = map_start + delta_x;
                double z = map_start + delta_z;
                Entity treeEntity = CreateTree(x, z, 0);
                entities.Add(NextId, treeEntity);
            }
            return entities;
        }
        public static Entity CreateTree(double x, double z, int burned)
        {
            var writeAuth = new Map<uint, WorkerRequirementSet>();
            writeAuth.Add(Tree.ComponentId, TreeWorker);
            var entity = BaseEntity(x, z, "Tree", writeAuth);
            entity.Add(new Tree.Data(9));
            return entity;
        }

        public static Entity BaseEntity(double x, double z, string name, Map<uint, WorkerRequirementSet> writeAcl)
        {
            var entity = new Entity();
            entity.Add(new Position.Data(new Coordinates(x, 0, z)));
            entity.Add(new Persistence.Data());
            entity.Add(new Metadata.Data(name));
            var aclData = new EntityAclData();
            aclData.readAcl = TreeWorker;
            aclData.componentWriteAcl = writeAcl;
            entity.Add(new EntityAcl.Data(aclData));
            return entity;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: mono Managed.exe receptionist <hostname> <port> <worker_id>");
            Console.WriteLine("Connects to SpatialOS");
            Console.WriteLine("    <hostname>      - hostname of the receptionist to connect to.");
            Console.WriteLine("    <port>          - port to use");
            Console.WriteLine("    <worker_id>     - name of the worker assigned by SpatialOS.");
        }

        private static Connection ConnectWithReceptionist(string hostname, ushort port,
            string workerId, ConnectionParameters connectionParameters)
        {
            Connection connection;

            // You might want to change this to true or expose it as a command-line option
            // if using `spatial cloud connect external` for debugging
            connectionParameters.Network.UseExternalIp = false;
            connectionParameters.EnableProtocolLoggingAtStartup = true;
            connectionParameters.ProtocolLogging.LogPrefix = "c:/logs/improbable/logs/" + workerId + "-log-";


            using (var future = Connection.ConnectAsync(hostname, port, workerId, connectionParameters))
            {
                connection = future.Get();
            }

            connection.SendLogMessage(LogLevel.Info, LoggerName, "Successfully connected using the Receptionist");

            return connection;
        }
    }
}