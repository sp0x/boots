using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing.Tokenizers
{
    public class FeatureToolsTokenizer : ITokenizer
    {
        private List<TokenDefinition> _tokenDefinitions;
        private DataIntegration[] _integrations;
        private DataIntegration _currentIntegration;

        public FeatureToolsTokenizer(params DataIntegration[] integrations)
        {
            var defs = new FeatureToolsTokenDefinitions();
            _tokenDefinitions = new List<TokenDefinition>();
            _tokenDefinitions.AddRange(defs.GetAll());
            _integrations = integrations;
            foreach (var ign in _integrations)
            {
                var ignToken = new TokenDefinition(TokenType.DataSource, Regex.Escape(ign.Name), 1);
                _tokenDefinitions.Add(ignToken);
            }
        }
        public FeatureToolsTokenizer(FeatureToolsTokenDefinitions toks, params DataIntegration[] integrations)
        {
            _tokenDefinitions = new List<TokenDefinition>();
            _tokenDefinitions.AddRange(toks.GetAll());
            _integrations = integrations;
        }

        public IEnumerable<DslToken> Tokenize(string query)
        {
            var reader = new StringReader(query);
            return Tokenize(reader);
        }
        public IEnumerable<DslToken> Tokenize(string query, ref int cReadTokens)
        {
            var reader = new StringReader(query);
            return Tokenize(reader, ref cReadTokens);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strReader"></param>
        /// <returns></returns>
        public IEnumerable<DslToken> Tokenize(StringReader strReader)
        {
            int tmpint = 0;
            return Tokenize(strReader, ref tmpint);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strReader"></param>
        /// <returns></returns>
        public IEnumerable<DslToken> Tokenize(StringReader strReader, ref int cReadTokens)
        {
            var tokenMatches = FindTokenMatches(strReader).ToList();
            var definitions = tokenMatches.GroupBy(x => new TokenPosition(x.Line, (uint)x.StartIndex),
                    new TokenPositionComparer())
                .OrderBy(x => x.Key.Line)
                .ThenBy(x => x.Key.Position)
                .ToList();

            TokenMatch lastMatch = null;
            var output = new Stack<DslToken>();
            bool isSubstring = false;
            DataIntegration matchedIntegration = null;
            for (int i = 0; i < definitions.Count; i++)
            {
                var orderedEnumerable = definitions[i].OrderBy(x => x.Precedence);
                var bestMatch = orderedEnumerable.First();
                DataIntegration tokensTargetDataset=null;
                if (lastMatch != null && bestMatch.StartIndex < lastMatch.EndIndex
                                      && bestMatch.Line == lastMatch.Line)
                {
                    //Validate if it's ok for the last expression to contain the current one;
                    var isValid = HandleExpressionSubstring(lastMatch, bestMatch, out tokensTargetDataset);
                    isSubstring = true;
                    if (!isValid) continue;
                }

                if (bestMatch.TokenType == TokenType.DatasetTime)
                {
                    if (isSubstring)
                    {
                        if (tokensTargetDataset == null) throw new Exception("Target DataSet not found!");
                        var timeTokens = ConstructTimeTokens(tokensTargetDataset, bestMatch.Line, bestMatch.StartIndex, i, output);
                        foreach (var tt in timeTokens) output.Push(tt);
                        continue;
                    }
                    else
                    {
                        var timeTokens = ConstructTimeTokens(_currentIntegration, bestMatch.Line, bestMatch.StartIndex, i, output);
                        foreach (var tt in timeTokens) output.Push(tt);
                        i += 1;
                        continue;
                    }
                }
                if (bestMatch.TokenType == TokenType.First)
                {
                    var nextBestMatch = ((definitions.Count - 1) == i) ? null : definitions[i + 1];
                    if (nextBestMatch != null)
                    {
                        var nextTokens = definitions.Skip(i+2).Select(x=> x.FirstOrDefault()).ToList();
                        var expValue = GetParameterSymbol(bestMatch, nextBestMatch, nextTokens, out matchedIntegration, ref cReadTokens);
                        i += cReadTokens;
                        _currentIntegration = matchedIntegration;
                        if (String.IsNullOrEmpty(expValue)) continue;
                        //Get the subtoken
                        int iReadTokens = 0;
                        var subTokens = ConstructFirstElementTokens(bestMatch, expValue, ref iReadTokens);
                        if (subTokens == null || subTokens.Count() == 0)
                        {
                            continue;
                        }
                        foreach (var tt in subTokens) output.Push(tt);
                        //We skip the subtokens, so that we can continue.
                        i += iReadTokens;
                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }


                var token = new DslToken(bestMatch.TokenType, bestMatch.Value, bestMatch.Line)
                {
                    Position = (uint)bestMatch.StartIndex
                };
                output.Push(token);
                //yield return token;

                lastMatch = bestMatch;
            }
            cReadTokens = definitions.Count;
            return output.Reverse();
        }

        private string GetParameterSymbol(TokenMatch bestMatch, IGrouping<TokenPosition, TokenMatch> nextBestMatches,
            List<TokenMatch> nextTokens,
            out DataIntegration dataIntegration,
            ref int cntReadTokens)
        {
            int offset = 0;
            var nextDefsSorted = nextBestMatches.OrderBy(x => x.Precedence).ToList();
            dataIntegration = null;
            while ((nextDefsSorted.Count)>offset)
            {
                var nextDef = nextDefsSorted[offset];
                var nextDefVal = nextDef.Value;
                if (offset == 0)
                {
                    var pTokenIndex = nextDefVal.IndexOf(bestMatch.Value);
                    if (pTokenIndex > 0) return null;
                    nextDefVal = nextDefVal.Substring(bestMatch.Value.Length);
                }
                string outputExpression;
                if (ParseParameterSubstring(nextTokens, nextDefVal, out dataIntegration, out outputExpression, ref cntReadTokens))
                {
                    return outputExpression;
                    break;
                }
                offset++;
            }
            var nextBestMatch = nextBestMatches.OrderBy(x => x.Precedence).First();
            var expValue = nextBestMatch.Value;
            expValue = expValue.Substring(bestMatch.Value.Length);
            return expValue;
        }

        private bool ParseParameterSubstring(List<TokenMatch> nextTokens, 
            string nextDefVal,
            out DataIntegration matchingIntegration,
            out string outputExpValue,
            ref int cntReadTokens)
        {
            matchingIntegration = _integrations.FirstOrDefault(x => x.Name.StartsWith(nextDefVal));
            outputExpValue = null;
            if (matchingIntegration != null)
            {
                var isFullMatch = matchingIntegration.Name == nextDefVal;
                if (!isFullMatch && nextTokens != null && nextTokens.Count > 0)
                {
                    var strLeft = matchingIntegration.Name.Substring(nextDefVal.Length);
                    for (int intx = 0; intx < nextTokens.Count; intx++)
                    {
                        cntReadTokens++;
                        TokenMatch tokenMatch = nextTokens[intx];
                        if (strLeft.StartsWith(tokenMatch.Value))
                        {
                            strLeft = strLeft.Substring(tokenMatch.Value.Length);
                        }
                        else if (tokenMatch.Value.EndsWith(strLeft))
                        {
                            TokenMatch nextBest = null;
                            for (int xt = intx; xt < nextTokens.Count; xt++)
                            {
                                if (nextTokens[xt].Line >= tokenMatch.Line &&
                                    nextTokens[xt].StartIndex >= tokenMatch.EndIndex)
                                {
                                    cntReadTokens += xt-1;
                                    nextBest = nextTokens[xt];
                                    break;
                                }
                            }
                            if (nextBest == null) continue;
                            outputExpValue = nextBest?.Value;
                            //outputExpValue = tokenMatch.Value;
                            break;
                        }
                        else
                        {
                            outputExpValue = tokenMatch.Value;
                            break;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private IEnumerable<DslToken> ConstructTimeTokens(DataIntegration tkTargetDataset, uint startLine, int startIndex,
            int i, Stack<DslToken> output)
        {
            var matches = new List<DslToken>();
            if(output.Count>0) output.Pop();
            var timeFn = new DslToken(TokenType.Symbol, "dstime", startLine) {Position = (uint)startIndex };
            var obrk = new DslToken(TokenType.OpenParenthesis, "(", startLine) {Position = (uint)startIndex + 4};
            var paramsVal = "";
            paramsVal += tkTargetDataset.Name;
            var dbSymbol = new DslToken(TokenType.Symbol, paramsVal, startLine) {  Position =(uint)(startIndex + 5)};
            var cbrk = new DslToken(TokenType.CloseParenthesis, ")", startLine) {Position = (uint)(startIndex + 4 + paramsVal.Length) };
            matches.AddRange(new []{ timeFn, obrk, dbSymbol, cbrk });
            return matches;
        }

        /// <summary>
        /// Construct a first({innerExpValue}) call
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        /// <param name="innerExpValue"></param>
        /// <param name="cntOfReadTokens"></param>
        /// <returns></returns>
        private IEnumerable<DslToken> ConstructFirstElementTokens(TokenMatch parent, string innerExpValue, ref int cntOfReadTokens)
        {
            var subTokens = Tokenize(innerExpValue, ref cntOfReadTokens);
            var output = new List<DslToken>();
            var timeFn = new DslToken(TokenType.Symbol, "first", parent.Line) { Position = (uint)parent.StartIndex };
            var obrk = new DslToken(TokenType.OpenParenthesis, "(", parent.Line) { Position = (uint)parent.StartIndex + 4 };
            output.AddRange(new[] {timeFn, obrk});
            DslToken lastSubTok = null;
            foreach (var subtok in subTokens)
            {
                output.Add(subtok);
                lastSubTok = subtok;
            }
            var cbrk = new DslToken(TokenType.CloseParenthesis, ")", parent.Line) { Position = (uint)((int)lastSubTok.Position+ lastSubTok.ToString().Length) };
            output.AddRange(new[] {cbrk });
            return output;
        }

        private bool HandleExpressionSubstring(TokenMatch parent, TokenMatch child, out DataIntegration targetDataSet)
        {
            targetDataSet = null;
            if (child.TokenType != TokenType.DatasetTime) return false;
            string pValue = parent.Value;
            var subIndex = pValue.LastIndexOf(child.Value);
            if (subIndex != (pValue.Length - child.Value.Length))
            {
                return false;
            }
            else
            {
                pValue = pValue.Substring(0, pValue.Length - child.Value.Length);
                var pValTokens = Tokenize(pValue);
                var foundDataSet = false;
                foreach (var subToken in pValTokens)
                {
                    if (subToken.TokenType != TokenType.Symbol) continue;
                    var dataIntegration = _integrations.FirstOrDefault(x => x.Name == subToken.Value);
                    if (dataIntegration!=null)
                    {
                        targetDataSet = dataIntegration;
                        foundDataSet = true; break;
                    }
                }
                return foundDataSet;
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="lqlText"></param>
        /// <returns></returns>
        private IEnumerable<TokenMatch> FindTokenMatches(StringReader lqlText)
        {
            //var tokenMatches = new List<TokenMatch>();
            string line;
            uint iLine = 1;
            int foundTokens = 0;
            while (null != (line = lqlText.ReadLine()))
            {
                foreach (var tokenDefinition in _tokenDefinitions)
                {
                    var tokenMatches = tokenDefinition.FindMatches(line).ToList();
                    foreach (var match in tokenMatches)
                    {
                        match.Line = iLine;
                        yield return match;
                        foundTokens++;
                    }
                    //tokenMatches.AddRange(collection);
                }
                iLine++;
            }
            if (foundTokens == 0)
            {
                foreach (var tokenDefinition in _tokenDefinitions)
                {
                    var tokenMatches = tokenDefinition.FindMatches(line).ToList();
                    foreach (var match in tokenMatches)
                    {
                        match.Line = iLine;
                        yield return match;
                        foundTokens++;
                    }
                    //tokenMatches.AddRange(collection);
                }
            }
            //return tokenMatches;
        }
    }
}