namespace datopus.Core.Entities.BigQuery;

public class BQDimension
{
    public bool Custom { set; get; }
    public string ApiName { set; get; }

    public string Name { set; get; }

    public BQDimension(bool custom, string apiName, string name)
    {
        Custom = custom;
        ApiName = apiName;
        Name = name;
    }
}
