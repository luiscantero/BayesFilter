using System;
using System.Collections.Generic;

namespace BayesFilter.Portable
{
    public class BFEngine : IDisposable
    {
        // Dictionaries (hash tables).
        private Dictionary<string, int> goodTokenOccurrence = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        private Dictionary<string, int> badTokenOccurrence = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        private Dictionary<string, double> badTokenProb = new Dictionary<string, double>(StringComparer.CurrentCultureIgnoreCase);

        private enum TrainType
        {
            Good,
            Bad
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
                return badTokenProb.Count;
            }
        }

        // Database connection object.
        //public object ConnectionObject { get; set; }

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
        public int SignificantTokens { get; set; } = 15;
        // Allowed charset for tokens, all other (visible) chars are separators.
        public string TokenCharset { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-!$€£¥ÀÁÂÃÄÅÇÈÉÊËÌÍÏÐÑÒÓÔÕÖÙÚÛÜÝßàáâãäåæçèéêëìíîïñòóôõöùúûüýÿ";
        // Prob for unkown token.
        public double UnkownBadProb { get; set; } = 0.5;

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // Clean up.
            //ConnectionObject = null;
            goodTokenOccurrence.Clear();
            goodTokenOccurrence = null;
            badTokenOccurrence.Clear();
            badTokenOccurrence = null;
            badTokenProb.Clear();
            badTokenProb = null;
        }

        // Classifies text by calculating its probability of being bad.
        public double GetBadProbability(string text)
        {
            double totalProb = 0;

            Dictionary<string, double> tokenProbDict = GetTokenProbDict(text);
            Dictionary<string, double> significantTokenDict = GetMostSignificantTokens(tokenProbDict);

            string[] keys = GetKeys(significantTokenDict);

            // Add all probabilities
            for (int i = 0; i < keys.Length; i++)
            {
                totalProb += significantTokenDict[keys[i]];
            }

            // Calc return value.
            double badProb = keys.Length > 0 ? totalProb / keys.Length : UnkownBadProb;

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
            var probDict = new Dictionary<string, double>();

            string[] keys = GetKeys(tokenProbDict);

            // Consider only tokens that occured a minimum amount of times.
            for (int i = 0; i < keys.Length; i++)
            {
                if (badTokenProb.ContainsKey(keys[i]))
                {
                    // Token does not meet the minimum occurrence requirement, remove it.
                    if (goodTokenOccurrence[keys[i]] + badTokenOccurrence[keys[i]] < MinTokenOccurrence)
                    {
                        tokenProbDict.Remove(keys[i]);
                    }
                }
                else // Unkown token, remove it.
                {
                    tokenProbDict.Remove(keys[i]);
                }
            }

            // Update key array.
            keys = GetKeys(tokenProbDict);

            // Return partial list if more than max number of significant (bad) tokens.
            if (keys.Length > SignificantTokens)
            {
                probDict = GetMostSignificantTokens(tokenProbDict, keys);
            }
            else
            {
                // Return full list if less than max number of significant (bad) tokens.
                probDict = tokenProbDict;
            }

            // Return.
            return probDict;
        }

        private Dictionary<string, double> GetMostSignificantTokens(Dictionary<string, double> tokenProbDict,  string[] keys)
        {
            var probDict = new Dictionary<string, double>();
            var arrSigToken = new string[SignificantTokens];
            var arrSigProb = new double[SignificantTokens];

            // Add first SignificantTokens tokens to temp array.
            for (int i = 0; i < SignificantTokens; i++)
            {
                arrSigToken[i] = keys[i]; // Token.
                arrSigProb[i] = tokenProbDict[keys[i]]; // Prob.
            }

            // Search for more significant tokens.
            for (int i = SignificantTokens; i < keys.Length; i++)
            {
                // Compare them one by one with the tokens already in the array.
                for (int x = 0; x < SignificantTokens; x++)
                {
                    // Token has higher probability of being bad, use it instead.
                    if (tokenProbDict[keys[i]] > arrSigProb[x])
                    {
                        arrSigToken[x] = keys[i];
                        arrSigProb[x] = tokenProbDict[keys[i]];
                        break;
                    }
                }
            }

            // Make a dictionary.
            for (int i = 0; i < SignificantTokens; i++)
            {
                probDict.Add(arrSigToken[i], arrSigProb[i]);
            }

            return probDict;
        }

        // Prob of something good, taking into account the number of good and bad experiences.
        private double GetProbGood(int goodCount, int badCount)
        {
            // Return prob or no data.
            return goodCount > 0 || badCount > 0 ? goodCount / (double)(goodCount + badCount) : UnkownBadProb;
        }

        // Returns a dictionary containing all found tokens and their prob.
        private Dictionary<string, double> GetTokenProbDict(string text)
        {
            var probDict = new Dictionary<string, double>(StringComparer.CurrentCultureIgnoreCase);

            // Clean up text.
            text = text.Replace(Environment.NewLine, " ");

            // Remove invisible chars.
            for (int i = 0; i < 31; i++)
            {
                text = text.Replace(((char)i).ToString(), "");
            }

            int begin = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (TokenCharset.IndexOf(text.Substring(i, 1)) == -1 || i == text.Length - 1) // Separator found.
                {
                    int pad = i == text.Length - 1 ? 1 : 0;
                    string strTemp = text.Substring(begin, i - begin + pad);

                    // Add token and its probability.
                    if (!string.IsNullOrEmpty(strTemp))
                    {
                        if (!probDict.ContainsKey(strTemp)) // Token not yet in list.
                        {
                            double prob;

                            if (badTokenProb.ContainsKey(strTemp)) // Known token.
                            {
                                prob = badTokenProb[strTemp];
                            }
                            else // Unknown token.
                            {
                                prob = UnkownBadProb;
                            }

                            probDict.Add(strTemp, prob); // Add with prob.
                        }
                    }

                    begin = i + 1;
                }
            }

            return probDict;
        }

        private void TrainDict(Dictionary<string, double> dicTable, TrainType trainType)
        {
            Dictionary<string, int> trainDict, oppositeDict;

            switch (trainType)
            {
                case TrainType.Good: // Good.
                    trainDict = goodTokenOccurrence;
                    oppositeDict = badTokenOccurrence;
                    GoodEventCount++;
                    break;

                default: // Bad.
                    trainDict = badTokenOccurrence;
                    oppositeDict = goodTokenOccurrence;
                    BadEventCount++;
                    break;
            }

            string[] arrKeys = GetKeys(dicTable);

            // Get all tokens and their prob and put them in the dictionary.
            for (int i = 0; i < arrKeys.Length; i++)
            {
                if (badTokenProb.ContainsKey(arrKeys[i])) // Token exists.
                {
                    // Increase count by one regardless of number of ocurrences in text.
                    trainDict[arrKeys[i]]++;
                }
                else // Add new token.
                {
                    trainDict.Add(arrKeys[i], 1);
                    oppositeDict.Add(arrKeys[i], 0);
                    badTokenProb.Add(arrKeys[i], 0);
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

            string[] keys = GetKeys(badTokenProb);

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

            for (int i = 0; i < keys.Length; i++)
            {
                // Prob that a token is good.
                probBAN = GetProbGood(goodTokenOccurrence[keys[i]], badTokenOccurrence[keys[i]]);

                // Prob that a token is bad.
                probBA = 1 - probBAN;

                badTokenProb[keys[i]] = CalcBayes(probBA, probPA, probBAN, probAN);
            }
        }

        //// Load all tokens from a DB.
        //public void DBLoad()
        //{
        //    object objRecordSet;
        //    string sql;

        //    // Load tokens, counts and probabilities from a DB.
        //    sql = "SELECT * FROM tbl_Tokens";
        //    objRecordSet = ConnectionObject.Execute(sql);

        //    while (!objRecordSet.EOF)
        //    {
        //        goodTokenOccurrence.Add((string)objRecordSet("tok_Name"), (int)objRecordSet("tok_CountGood"));
        //        badTokenOccurrence.Add((string)objRecordSet("tok_Name"), (int)objRecordSet("tok_CountBad"));
        //        badTokenProb.Add((string)objRecordSet("tok_Name"), (double)objRecordSet("tok_BayProb"));

        //        objRecordSet.MoveNext();
        //    }

        //    objRecordSet = null;

        //    // Load counters.
        //    sql = "SELECT * FROM tbl_Counter";
        //    objRecordSet = ConnectionObject.Execute(sql);

        //    if (!objRecordSet.EOF)
        //    {
        //        GoodEventCount = objRecordSet("cnt_Good");
        //        BadEventCount = objRecordSet("cnt_Bad");
        //    }

        //    objRecordSet = null;
        //} // DBLoad.

        //// Save all tokens to a DB.
        //public void DBSave()
        //{
        //    object objRecordSet;
        //    string sql;

        //    // Clear tables.
        //    sql = "DELETE FROM tbl_Tokens";
        //    ConnectionObject.Execute(sql);
        //    sql = "DELETE FROM tbl_Counter";
        //    ConnectionObject.Execute(sql);

        //    objRecordSet = CreateObject("ADODB.Recordset");

        //    objRecordSet.Open("tbl_Tokens", ConnectionObject, 0, 3); // adOpenForwardOnly, adLockOptimistic

        //    string[] arrKeys = GetKeys(badTokenProb);

        //    // Save tokens, counts and probabilities to a DB.
        //    for (int i = 0; i < arrKeys.Length; i++)
        //    {
        //        objRecordSet.AddNew();
        //        if (arrKeys[i].Length > MaxTokenLength)
        //        {
        //            arrKeys[i] = arrKeys[i].Substring(0, MaxTokenLength);
        //        }

        //        objRecordSet("tok_Name") = arrKeys[i];
        //        objRecordSet("tok_CountGood") = goodTokenOccurrence[arrKeys[i]];
        //        objRecordSet("tok_CountBad") = badTokenOccurrence[arrKeys[i]];
        //        objRecordSet("tok_BayProb") = badTokenProb[arrKeys[i]];

        //        objRecordSet.Update();
        //    }

        //    objRecordSet.Close();

        //    objRecordSet = null;

        //    // Save counters.
        //    sql = "INSERT INTO tbl_Counter(cnt_Good, cnt_Bad) VALUES(" + GoodEventCount + ", " + BadEventCount + ")";
        //    ConnectionObject.Execute(sql);
        //} // DBSave.
    }
}
