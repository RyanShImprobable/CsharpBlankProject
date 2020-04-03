using System.Collections;
using System.Collections.Generic;
using Improbable;
using Improbable.Worker;

namespace Managed
{
    public class ComponentMap<MetaClass> : IEnumerable<KeyValuePair<EntityId, IComponentData<MetaClass>>> where MetaClass : IComponentMetaclass
    {
        private readonly HashSet<EntityId> Authority = new HashSet<EntityId>();

        private readonly Dictionary<EntityId, IComponentData<MetaClass>> Components =
            new Dictionary<EntityId, IComponentData<MetaClass>>();

        public ComponentMap(Dispatcher Dispatcher)
        {
            
        }

        public Dictionary<EntityId, IComponentData<MetaClass>>.KeyCollection Keys
        {
            get { return Components.Keys; }
        }

        public Dictionary<EntityId, IComponentData<MetaClass>>.ValueCollection Values
        {
            get { return Components.Values; }
        }

        public bool ContainsKey(EntityId id)
        {
            return Components.ContainsKey(id);
        }

        public IComponentData<MetaClass> Get(EntityId id)
        {
            return Components[id];
        }

        public bool TryGetValue(EntityId id, out IComponentData<MetaClass> Component)
        {
            return Components.TryGetValue(id, out Component);
        }

        public bool HasAuthority(EntityId Id)
        {
            return Authority.Contains(Id);
        }

        private void SetAuthority(AuthorityChangeOp AuthorityChange)
        {
            if (AuthorityChange.Authority == Improbable.Worker.Authority.Authoritative)
            {
                Authority.Add(AuthorityChange.EntityId);
            }
            else
            {
                Authority.Remove(AuthorityChange.EntityId);
            }
        }


        private void UpdateComponent(ComponentUpdateOp<MetaClass> Update)
        {
            if (!HasAuthority(Update.EntityId) && Components.ContainsKey(Update.EntityId))
            {
                //Update.Update.ApplyTo(Components[Update.EntityId]);
            }
        }

        private void AddComponent(AddComponentOp<MetaClass> Add)
        {
            //Components.Add(Add.EntityId, Add.Data);
        }


        private void RemoveEntity(RemoveEntityOp RemoveEntityOp)
        {
            if (Components.ContainsKey(RemoveEntityOp.EntityId))
            {
                Components.Remove(RemoveEntityOp.EntityId);
            }
        }

        public IEnumerator<KeyValuePair<EntityId, IComponentData<MetaClass>>> GetEnumerator()
        {
            return Components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
