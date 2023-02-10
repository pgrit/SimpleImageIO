using SimpleImageIO.Benchmark;

ColorBench.BenchLerp(1000000);

IOBench.BenchIO();

ImageOpsBench.BenchComputePercentile();
ImageOpsBench.BenchGetSetPixel();
ImageOpsBench.BenchErrors();
ImageOpsBench.BenchSplatting();

FiltersBench.BenchBoxFilter();
FiltersBench.BenchBoxFilter3();
FiltersBench.BenchDilationFilter();
FiltersBench.BenchErosionFilter();
FiltersBench.BenchMedianFilter();
FiltersBench.BenchGaussFilter();
