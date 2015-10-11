using BayesFilter.Portable;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BayesFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var bayes = new BFEngine(new ConsolePlatformServices()))
            {
                // Train 5 times to reach MinTokenOccurrence.
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");

                // Train 5 times to reach MinTokenOccurrence.
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");

                Console.WriteLine($"Good events: {bayes.GoodEventCount} | Bad events: {bayes.BadEventCount} | Token Count: {bayes.TokenCount}.");

                string test;
                test = "a b c d e f g h i j k l m n o p q r s t u v w x y z"; // 26
                //test = "a b c d e f g h i j p q r s"; // 14
                //test = "a b c d e f g h i j p q r s t"; // 15
                Console.WriteLine($"Evaluate \"{test}\": {bayes.GetBadProbability(test):0.00}");

                test = "a b c d e f g h i j k l m n o p q";
                bayes.AutoTrain = true; // Only happens if result passes threshold test.
                Console.WriteLine($"Evaluate \"{test}\": {bayes.GetBadProbability(test):0.00}");
                Console.WriteLine($"Evaluate \"{test}\": {bayes.GetBadProbability(test):0.00}");
                Console.WriteLine($"Evaluate \"{test}\": {bayes.GetBadProbability(test):0.00}");

                Task t = bayes.SaveAsync();
                t.Wait();

                t = bayes.LoadAsync();
                t.Wait();

                // Perf test.
                //RunPerfTest(bayes, test);

                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey(true);
            }
        }

        private static void RunPerfTest(BFEngine bayes, string test)
        {
            int length = 100000;

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < length; i++)
            {
                double prob = bayes.GetBadProbability(test);
            }
            watch.Stop();
            Console.WriteLine($"Ellapsed: {watch.ElapsedMilliseconds} ms.");
        }
    }
}
