namespace GameAnalytics.DTOs;

public record MetricPoint(DateTime Date, double Value);

public record DauWauMauResponse(
	List<MetricPoint> Dau,
	List<MetricPoint> Wau,
	List<MetricPoint> Mau
);

public record RevenueMetricsResponse(
	List<MetricPoint> DailyRevenue,
	double TotalRevenue,
	double Arpu,
	double Arppu,
	int PayingUsers,
	double PayerConversionRate
);

public record RetentionPoint(int Day, double Rate);

public record RetentionResponse(
	List<RetentionPoint> Classic,
	List<RetentionPoint> Rolling,
	List<CohortRow> CohortTable
);

public record CohortRow(
	string CohortLabel,
	int CohortSize,
	List<double?> RetentionRates
);

public record SessionMetricsResponse(
	List<MetricPoint> AvgSessionDuration,
	List<MetricPoint> SessionsPerUser,
	double OverallAvgDurationSeconds
);

public record LabelValue(string Label, double Value);

public record UserAcquisitionResponse(
	List<MetricPoint> DailyNewUsers,
	int TotalNewUsers,
	List<LabelValue> ByCountry,
	List<LabelValue> ByPlatform
);
