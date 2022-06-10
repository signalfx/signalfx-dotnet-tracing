namespace SignalFx.OverheadTest.Results;

internal record K6Results(
    float IterationDurationAvg,
    float IterationDurationP95,
    float RequestDurationAvg,
    float RequestDurationP95);
