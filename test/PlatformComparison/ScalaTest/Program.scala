import scala.collection.immutable.HashMap;

object Benchmarker {
    def runTest(testSize: Int, toPrint: Boolean) : (Long, Long) = {
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

        val timeIntervalGet = (endTime - startTime) / 1000000; // To milliseconds
        val timeIntervalWrite = (endWriteTime - writeStartTime) / 1000000; // To milliseconds

        if (toPrint) {
            println(s"| $testSize | $timeIntervalGet | $timeIntervalWrite |");
        }

        return (timeIntervalGet, timeIntervalWrite);
    }
}

object Program extends App {
    val testSizes = List(
        100,
        1000,
        10000,
        100000,
        500000,
        1000000,
        5000000,
        10000000,
        50000000
    )

    var headingString = testSizes.foldLeft("| Lang | Operation | TestSize | ") {
        (acc, e) => acc + e.toString + " | "
    };

    println(headingString);

    Benchmarker.runTest(100, false); // Warmup

    val testResults = testSizes.map(testSize => Benchmarker.runTest(testSize, false));

    val testResultGetTime = testResults.foldLeft ("| Scala | TryFind | ") {
        (acc, times) => 
            val (getTime, _) = times;
            acc + getTime.toString + " | ";
    };

    println(testResultGetTime);

    val testResultOfSeqTime = testResults.foldLeft ("| Scala | OfSeq | ") {
        (acc, times) => 
            val (_, ofSeqTime) = times;
            acc + ofSeqTime.toString + " | ";
    };

    println(testResultOfSeqTime);
}