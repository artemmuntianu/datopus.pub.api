namespace datopus.Core.Entities.BigQuery;

public class BQMetric
{
    public bool Custom { set; get; }
    public string ApiName { set; get; }

    public string Name { set; get; }

    public BQMetric(bool custom, string apiName, string name)
    {
        Custom = custom;
        ApiName = apiName;
        Name = name;
    }
}
