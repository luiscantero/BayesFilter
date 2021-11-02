using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace LC.BayesFilter
{
    public sealed class BfEngine : IDisposable
    {
        private IPlatformServices _platformServices;

        // Dictionaries (hash tables).
        private Dictionary<string, int> _goodTokenOccurrence = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        private Dictionary<string, int> _badTokenOccurrence = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        private Dictionary<string, double> _badTokenProb = new Dictionary<string, double>(StringComparer.CurrentCultureIgnoreCase);

        private enum TrainType
        {
            Good,
            Bad,
        }

        // Good event count.
        public int GoodEventCount { get; private set; }
        // Bad event count.
        public int BadEventCount { get; private set; }
        // Token count.
        public int TokenCount
        {
            get
            {
                return _badTokenProb.Count;
            }
        }

        // Auto train when classifying based on good and bad thresholds.
        public bool AutoTrain { get; set; } = false;
        // Lower bound for bad tokens, for auto-learning purposes.
        public double AutoTrainBadThreshold { get; set; } = 0.9;
        // Upper bound for good tokens, for auto-learning purposes.
        public double AutoTrainGoodThreshold { get; set; } = 0.1;
        // Max string length for the tokens (for DB saving).
        public int MaxTokenLength { get; set; } = 255;
        // Min number of occurrences for a token to be considered when evaluating.
        public int MinTokenOccurrence { get; set; } = 5;
        // Max number of tokens to consider when evaluating a string.
        public int SignificantTokens { get; set; } = 15; // n-gram.
        // Allowed charset for tokens, all other (visible) chars are separators.
        public string TokenCharset { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-!$€£¥ÀÁÂÃÄÅÇÈÉÊËÌÍÏÐÑÒÓÔÕÖÙÚÛÜÝßàáâãäåæçèéêëìíîïñòóôõöùúûüýÿ";
        // Prob for unkown token.
        public double UnkownBadProb { get; set; } = 0.5;

        public BfEngine(IPlatformServices platformServices)
        {
            _platformServices = platformServices;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // Clean up.
            //ConnectionObject = null;
            _goodTokenOccurrence.Clear();
            _goodTokenOccurrence = null;
            _badTokenOccurrence.Clear();
            _badTokenOccurrence = null;
            _badTokenProb.Clear();
            _badTokenProb = null;
        }

        // Classifies text by calculating its probability of being bad.
        public double GetBadProbability(string text)
        {
            Dictionary<string, double> tokenProbDict = GetTokenProbDict(text);
            Dictionary<string, double> significantTokenDict = GetMostSignificantTokens(tokenProbDict);

            // Add all probabilities
            double totalProb = significantTokenDict.Values.Sum();

            double badProb = significantTokenDict.Count > 0 ?
                totalProb / significantTokenDict.Count : // Total prob / number of tokens.
                UnkownBadProb; // Unknown.

            // Auto train only when enabled and if the minimum amount of significant tokens have been found.
            RunAutoTrain(tokenProbDict, significantTokenDict.Count, badProb);

            // Clean up.
            tokenProbDict = null;
            significantTokenDict = null;

            return badProb;
        }

        private void RunAutoTrain(Dictionary<string, double> tokenProbDict, int significantTokenCount, double badProb)
        {
            if (AutoTrain && significantTokenCount == SignificantTokens)
            {
                if (badProb >= AutoTrainBadThreshold) // Train as bad.
                {
                    TrainDict(tokenProbDict, TrainType.Bad);
                }
                else
                {
                    if (badProb <= AutoTrainGoodThreshold) // Train as good.
                    {
                        TrainDict(tokenProbDict, TrainType.Good);
                    }
                }
            }
        }

        // Train text as good.
        public void TrainAsGood(string text)
        {
            TrainDict(GetTokenProbDict(text), TrainType.Good);
        }

        // Train text as bad.

        public void TrainAsBad(string text)
        {
            TrainDict(GetTokenProbDict(text), TrainType.Bad);
        }

        // probBA: Prob that a token is bad.
        // probA: Prob of bad ocurring.
        // probBAN: Prob that a token is good.
        // probAN: Prob of good occuring.
        private static double CalcBayes(double probBA, double probA, double probBAN, double probAN)
        {
            return probBA * probA / (probBA * probA + probBAN * probAN);
        }

        private static string[] GetKeys<T>(Dictionary<string, T> dict)
        {
            var keys = new string[dict.Keys.Count];
            dict.Keys.CopyTo(keys, 0);

            return keys;
        }

        // Returns most significant tokens, measured by their probability of being bad.
        private Dictionary<string, double> GetMostSignificantTokens(Dictionary<string, double> tokenProbDict)
        {
            tokenProbDict = RemoveInsignificantTokens(tokenProbDict);

            // Return list with max number of significant (bad) tokens.
            return GetTopSignificantTokens(tokenProbDict);
        }

        private Dictionary<string, double> RemoveInsignificantTokens(Dictionary<string, double> tokenProbDict)
        {
            var query = from token in tokenProbDict
                            // Remove unkown tokens.
                        where _badTokenProb.ContainsKey(token.Key) &&
                              // Remove tokens that do not meet the minimum occurrence requirement.
                              _goodTokenOccurrence[token.Key] + _badTokenOccurrence[token.Key] >= MinTokenOccurrence
                        select token;

            return query.ToDictionary(t => t.Key, t => t.Value);
        }

        private Dictionary<string, double> GetTopSignificantTokens(Dictionary<string, double> tokenProbDict)
        {
            var query = from token in tokenProbDict
                        orderby token.Value descending
                        select token;

            return query.Take(SignificantTokens).ToDictionary(t => t.Key, t => t.Value);
        }

        // Prob of something good, taking into account the number of good and bad experiences.
        private double GetProbGood(int goodCount, int badCount)
        {
            return goodCount > 0 || badCount > 0 ?
                goodCount / (double)(goodCount + badCount) : // Prob.
                UnkownBadProb; // Unknown.
        }

        // Returns a dictionary containing all found tokens and their prob.
        private Dictionary<string, double> GetTokenProbDict(string text)
        {
            var probDict = new Dictionary<string, double>(); // Case insensitive compare not need.

            // Clean up text.
            text = CleanText(text);

            int begin = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (TokenCharset.IndexOf(text.Substring(i, 1)) == -1 || i == text.Length - 1) // Separator found.
                {
                    int pad = i == text.Length - 1 ? 1 : 0;
                    string token = text.Substring(begin, i - begin + pad);

                    // Add token and its probability.
                    if (!string.IsNullOrEmpty(token))
                    {
                        if (!probDict.ContainsKey(token)) // Token not yet in list.
                        {
                            double prob = _badTokenProb.ContainsKey(token) ?
                                _badTokenProb[token] : // Prob.
                                UnkownBadProb; // Unknown.

                            probDict.Add(token, prob); // Add with prob.
                        }
                    }

                    begin = i + 1;
                }
            }

            return probDict;
        }

        private static string CleanText(string text)
        {
            text = text.Replace(Environment.NewLine, " ");

            // Remove invisible chars.
            for (int i = 0; i < 31; i++)
            {
                text = text.Replace(((char)i).ToString(), "");
            }

            return text;
        }

        private void TrainDict(Dictionary<string, double> dicTable, TrainType trainType)
        {
            Dictionary<string, int> trainDict, oppositeDict;

            if (trainType == TrainType.Good) // Good.
            {
                trainDict = _goodTokenOccurrence;
                oppositeDict = _badTokenOccurrence;
                GoodEventCount++;
            }
            else // Bad.
            {
                trainDict = _badTokenOccurrence;
                oppositeDict = _goodTokenOccurrence;
                BadEventCount++;
            }

            // Get all tokens and their prob and put them in the dictionary.
            foreach (var key in dicTable.Keys)
            {
                if (_badTokenProb.ContainsKey(key)) // Token exists.
                {
                    // Increase count by one regardless of number of ocurrences in text.
                    trainDict[key]++;
                }
                else // Add new token.
                {
                    trainDict.Add(key, 1);
                    oppositeDict.Add(key, 0);
                    _badTokenProb.Add(key, 0);
                }
            }

            // Update Bayes probabilities for all tokens, necessary because changes in event count affect all probabilities
            UpdateBayProb();
        }

        // Recalculates the Bayes probability of something bad ocurring, updates the whole table.
        private void UpdateBayProb()
        {
            double probBAN, probBA, probAN, probPA;
            double correctionFactor = 0;

            probAN = GetProbGood(GoodEventCount, BadEventCount);
            probPA = 1 - probAN; // Prob bad.

            // Correct good prob, to avoid masses of bad events decreasing the good event probability too much.
            if (BadEventCount > GoodEventCount)
            {
                if (probAN > 0)
                {
                    correctionFactor = (probPA / probAN) / 2;
                }

                if (correctionFactor > 0)
                {
                    probAN = probAN * correctionFactor;
                }
            }

            string[] keys = GetKeys(_badTokenProb);

            for (int i = 0; i < keys.Length; i++)
            {
                // Prob that a token is good.
                probBAN = GetProbGood(_goodTokenOccurrence[keys[i]], _badTokenOccurrence[keys[i]]);

                // Prob that a token is bad.
                probBA = 1 - probBAN;

                _badTokenProb[keys[i]] = CalcBayes(probBA, probPA, probBAN, probAN);
            }
        }
        public async Task SaveAsync()
        {
            await _platformServices.SaveDictAsync("GoodTokenOccurrence", _goodTokenOccurrence);
            await _platformServices.SaveDictAsync("BadTokenOccurrence", _badTokenOccurrence);
            await _platformServices.SaveDictAsync("BadTokenProb", _badTokenProb);
        }

        public async Task LoadAsync()
        {
            _goodTokenOccurrence = await _platformServices.LoadDictAsync<Dictionary<string, int>>("GoodTokenOccurrence");
            _badTokenOccurrence = await _platformServices.LoadDictAsync<Dictionary<string, int>>("BadTokenOccurrence");
            _badTokenProb = await _platformServices.LoadDictAsync<Dictionary<string, double>>("BadTokenProb");
        }
    }
}