namespace GameAnalytics.Models;

public enum EventType { Custom, Session, Revenue, Error, Progression }
public enum Platform { iOS, Android, WebGL, Windows, Mac, Linux }
public enum ParameterType { EventParameter, UserProperty }
public enum DataType { String, Int, Float, Bool, DateTime }
public enum ReportChartType { Line, Bar, Doughnut, Table, Number }
