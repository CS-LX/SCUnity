using GameEntitySystem;
using TemplatesDatabase;

namespace SCUnity.Foundation.Dynamic
{
    public sealed class ProbeComponent : Component
    {
        public override void Load(ValuesDictionary values, IdToEntityMap map) => Entry.Events.Add("load:" + values.GetValue<int>("Number"));
        public override void Save(ValuesDictionary values, EntityToIdMap map) => values.SetValue("Number", 73);
        public override void OnEntityAdded() => Entry.Events.Add("component-added");
        public override void OnEntityRemoved() => Entry.Events.Add("component-removed");
        public override void Dispose() => Entry.Events.Add("component-disposed");
    }
}
