using System.Collections.Generic;

namespace Tools.EntitiesImporter
{
    public class EntityTableData : IEntityTableData
    {
        public EntityTableData(IReadOnlyDictionary<string, string> fields)
        {
            Fields = fields;
        }

        public IReadOnlyDictionary<string, string> Fields { get; }

        public bool TryGetValue(string columnName, out string value)
        {
            if (Fields == null)
            {
                value = null;
                return false;
            }

            return Fields.TryGetValue(columnName, out value);
        }
    }
}
