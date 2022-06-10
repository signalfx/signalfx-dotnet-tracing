namespace SignalFx.OverheadTest.Results;

internal record K6Results(
    double IterationDurationAvg,
    double IterationDurationP95,
    double RequestDurationAvg,
    double RequestDurationP95);
