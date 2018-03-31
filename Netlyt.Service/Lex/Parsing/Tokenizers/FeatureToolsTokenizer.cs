using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing.Tokenizers
{
    public class FeatureToolsTokenizer : ITokenizer
    {
        private List<TokenDefinition> _tokenDefinitions;
        private DataIntegration[] _integrations;

        public FeatureToolsTokenizer(params DataIntegration[] integrations)
        {
            var defs = new FeatureToolsTokenDefinitions();
            _tokenDefinitions = new List<TokenDefinition>();
            _tokenDefinitions.AddRange(defs.GetAll());
            _integrations = integrations;
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
        public IEnumerable<DslToken> Tokenize(string query, out int cReadTokens)
        {
            var reader = new StringReader(query);
            return Tokenize(reader, out cReadTokens);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strReader"></param>
        /// <returns></returns>
        public IEnumerable<DslToken> Tokenize(StringReader strReader)
        {
            int tmpint = 0;
            return Tokenize(strReader, out tmpint);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strReader"></param>
        /// <returns></returns>
        public IEnumerable<DslToken> Tokenize(StringReader strReader, out int cReadTokens)
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
                        var timeTokens = ConstructTimeTokens(tokensTargetDataset, bestMatch, i, output);
                        foreach (var tt in timeTokens) output.Push(tt);
                        continue;
                    }
                    else
                    {
                        throw new NotImplementedException("Time projection not supported for non-substring expressions. Use the format DataSetName_time");
                    }
                }
                if (bestMatch.TokenType == TokenType.First)
                {
                    var nextDefinitions = ((definitions.Count - 1) == i) ? null : definitions[i + 1];
                    if (nextDefinitions != null)
                    {
                        var nextBestMatch = nextDefinitions.OrderBy(x => x.Precedence).First();
                        var expValue = nextBestMatch.Value;
                        var pTokenIndex = expValue.IndexOf(bestMatch.Value);
                        if (pTokenIndex > 0) continue;
                        //Get the subtoken
                        expValue = expValue.Substring(bestMatch.Value.Length);
                        int iReadTokens = 0;
                        var subTokens = ConstructFirstElementTokens(bestMatch, nextBestMatch, expValue, out iReadTokens);
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

        private IEnumerable<DslToken> ConstructTimeTokens(DataIntegration tokensTargetDataset, TokenMatch bestMatch,
            int i, Stack<DslToken> output)
        {
            var matches = new List<DslToken>();
            output.Pop();
            var timeFn = new DslToken(TokenType.Symbol, "dstime", bestMatch.Line) {Position = (uint)bestMatch.StartIndex};
            var obrk = new DslToken(TokenType.OpenParenthesis, "(", bestMatch.Line) {Position = (uint)bestMatch.StartIndex+4};
            var paramsVal = "";
            paramsVal += tokensTargetDataset.Name;
            var dbSymbol = new DslToken(TokenType.Symbol, paramsVal, bestMatch.Line) {  Position =(uint)(bestMatch.StartIndex+5)};
            var cbrk = new DslToken(TokenType.CloseParenthesis, ")", bestMatch.Line) {Position = (uint)(bestMatch.StartIndex + 4 + paramsVal.Length) };
            matches.AddRange(new []{ timeFn, obrk, dbSymbol, cbrk });
            return matches;
        }


        private IEnumerable<DslToken> ConstructFirstElementTokens(TokenMatch parent, TokenMatch child, string expValue, out int cntOfReadTokens)
        {
            var subTokens = Tokenize(expValue, out cntOfReadTokens);
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
            var pValue = parent.Value;
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