import scala.collection.immutable.HashMap;

object Benchmarker {
    def runTest(testSize: Int) = {
        val a = Array.range(0, testSize);

        val writeStartTime = System.nanoTime();

        val m = a.map(i => i.toLong -> i.toLong).toMap;
        val endWriteTime = System.nanoTime();

        val startTime = System.nanoTime();

        val result = new Array[Long](testSize);

        for (i <- a) {
            result(i) = m.get(i.toLong).get
        }

        val endTime = System.nanoTime();

        var timeIntervalGet = (endTime - startTime) / 1000000; // To milliseconds
        var timeIntervalWrite = (endWriteTime - writeStartTime) / 1000000; // To milliseconds

        println(s"Scala Result [TestSize: $testSize, GetTime: $timeIntervalGet, FromSeqTime: $timeIntervalWrite]");
    }
}

object Program extends App {
    Benchmarker.runTest(100);
    Benchmarker.runTest(1000);
    Benchmarker.runTest(10000);
    Benchmarker.runTest(100000);
    Benchmarker.runTest(500000);
    Benchmarker.runTest(1000000);
    Benchmarker.runTest(5000000);
    Benchmarker.runTest(10000000);
}