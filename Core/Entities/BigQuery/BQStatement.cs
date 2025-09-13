namespace datopus.Core.Entities.BigQuery;

public class BQStatement
{
    public bool Groupable { set; get; }
    public required string Name { set; get; }

    public required string PseudoName { set; get; }
    public required string Inner { set; get; }
    public required string Outer { set; get; }
}
