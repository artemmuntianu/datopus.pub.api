namespace datopus.Core.Entities.BigQuery;


public class BQOrderBy
{
    public string FieldName { get; init; }
    public bool Desc { get; init; }

    public BQOrderBy(string fieldName, bool desc)
    {
        FieldName = fieldName;
        Desc = desc;
    }
}
