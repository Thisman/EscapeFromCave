using System.Collections.Generic;

public abstract class BaseEntityTableParser
{
    public abstract IEnumerable<IEntityTableData> Parse(string tableContent, string delimiter);
}
