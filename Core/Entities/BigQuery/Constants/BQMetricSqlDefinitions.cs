namespace datopus.Core.Entities.BigQuery.Constants;

public static class BQMetricSqlDefinitions
{
    public static readonly BQStatement Sessions = new()
    {
        Groupable = true,
        Inner =
            @"
        CONCAT(
        user_pseudo_id,
        (
          SELECT
            value.int_value
          FROM
            UNNEST (event_params)
          WHERE
            key = 'ga_session_id'
        )
      ) AS sessions
        ",
        Outer = "COUNT(DISTINCT sessions) AS sessions",
        Name = "sessions",
        PseudoName = "sessions",
    };

    public static readonly BQStatement Revenue = new()
    {
        Groupable = false,
        Inner = "SUM(ecommerce.purchase_revenue) AS revenue",
        Outer = "SUM(revenue) AS revenue",
        Name = "revenue",
        PseudoName = "revenue",
    };

    public static readonly BQStatement Events = new()
    {
        Groupable = false,
        Inner = "COUNT(event_name) AS events",
        Outer = "SUM(events) AS events",
        Name = "events",
        PseudoName = "events",
    };

    public static readonly BQStatement Users = new()
    {
        Groupable = true,
        Inner = "user_pseudo_id",
        Outer = "COUNT(DISTINCT user_pseudo_id) AS users",
        Name = "user_pseudo_id",
        PseudoName = "users",
    };
}
