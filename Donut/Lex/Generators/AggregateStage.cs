using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Donut.Crypto;
using Donut.Features;
using Donut.Lex.Expressions;
using Netlyt.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Donut.Lex.Generators
{
    public class AggregateStage
    {
        private IDonutScript _script;
        public AggregateStageType Type { get; set; }
        public IDonutFunction Function { get; set; }
        public List<AggregateStage> ChildStages { get; set; }

        public AggregateStage(IDonutScript script, IDonutFunction function)
        {
            Function = function;
            _script = script;
            ChildStages = new List<AggregateStage>();//new AggregateJobTree(script);
            switch (function.Type)
            {
                case DonutFunctionType.Project: Type = AggregateStageType.Project; break;
                case DonutFunctionType.GroupField: Type = AggregateStageType.Group; break;
            }
        }
        public IEnumerable<AggregateStage> GetGroupings()
        {
            return ChildStages.Where(x => x.Type == AggregateStageType.Group);
        }
        public IEnumerable<AggregateStage> GetProjections()
        {
            return ChildStages.Where(x => x.Type == AggregateStageType.Project);
        }
        public IEnumerable<AggregateStage> GetFilters()
        {
            return ChildStages.Where(x => x.Type == AggregateStageType.Match);
        }
        public AggregateStage Clone()
        {
            var nstage = new AggregateStage(_script, Function);
            nstage.Type = Type;
            nstage.ChildStages = new List<AggregateStage>(ChildStages.ToArray());
            return nstage;
        }

        public override string ToString()
        {
            return $"{Type}: {Function}";
        }

        /// <summary>
        /// Evaluates the value of this stage and it's substages
        /// </summary>
        /// <returns></returns>
        public string GetValue()
        {
            var template = Function.GetValue();
            if (string.IsNullOrEmpty(template))
            {
                return null;
            }
            var lstParameters = Function?.Parameters?.ToList();
            if (lstParameters == null) lstParameters = new List<IParameterExpression>();
            template = DonutFunctionToMongoAggregateElement(lstParameters, template);
            return template;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lstParameters"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public string DonutFunctionToMongoAggregateElement(List<IParameterExpression> lstParameters, string template)
        {
            for (int i = 0; i < lstParameters.Count; i++)
            {
                var parameter = lstParameters[i];
                object paramOutputObj;
                var pValue = parameter.Value;
                var argExpVisitor = new AggregateFeatureGeneratingExpressionVisitor(_script);
                //If we visit functions here, note that in the pipeline
                var paramStr = argExpVisitor.Visit(parameter as IExpression, out paramOutputObj);
                var subAggregateTree = argExpVisitor.AggregateTree;
                if (pValue is VariableExpression varValue && varValue.Member != null)
                {
                    var isRootIntegrationVar = varValue.Name == _script.GetRootIntegration().Name;
                    //Strip the root integration name in cases of [IntegrationName].[Function].
                    if (isRootIntegrationVar)
                    {
                        pValue = varValue.Member;
                    }
                }
                if (pValue is MemberExpression memberValue)
                {
                    pValue = memberValue.Parent;
                }
                ChildStages.AddRange(subAggregateTree.Stages);
                if (pValue is CallExpression || paramOutputObj is IDonutTemplateFunction)
                {
                    if (Function.Type == DonutFunctionType.GroupField)
                    {
                        if (paramOutputObj is IDonutTemplateFunction iDonutFn &&
                            iDonutFn.Type == DonutFunctionType.Project)
                        {
                            throw new System.Exception(
                                $"Function {Function} is a grouping and cannot have projection arguments.");
                        }
                    }
                }
                template = FormatDonutFnAggregateParameter(template, pValue, paramOutputObj, i, paramStr);
            }

            return template;
        }

        public static string FormatDonutFnAggregateParameter(string template, IExpression srcExpression, object srcDonutFn, int argIndex,
            string paramStr)
        {
            if ((srcExpression is CallExpression) || srcDonutFn is IDonutTemplateFunction)
            {
                template = template.Replace("\"{" + argIndex + "}\"", paramStr);
            }
            else
            {
                template = template.Replace("{" + argIndex + "}", $"${paramStr}");
            }
            return template;
        }

        public string WrapValueWithRoot(string mName)
        {
            var value = GetValue();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            try
            {
                var jsdoc = JObject.Parse(value);
                var wrapper = new JObject();
                wrapper[mName] = jsdoc;
                var output = wrapper.ToString(Formatting.None);
                output = output.Replace("\"", "\"\"");
                output = $"@\"{output}\"";
                return output;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return null;
            }
        }

        public string WrapValue(bool asStringLiteral = true)
        {
            var value = GetValue();
            try
            {
                var jsdoc = JObject.Parse(value);
                var output = jsdoc.ToString(Formatting.None);
                output = output.Replace("\"", "\"\"");
                if (asStringLiteral) output = $"@\"{output}\"";
                return output;
            }
            catch (Exception ex)
            {
                ex = ex;
                return null;
            }
        }

        public string GetHashCode()
        {
            var val = GetValue();
            var adhash = HashAlgos.Adler32(val);
            return adhash.ToString();
//            using (MD5 md5Hash = MD5.Create())
//            {
//                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(val));
//
//                // Create a new Stringbuilder to collect the bytes
//                // and create a string.
//                StringBuilder sBuilder = new StringBuilder();
//
//                // Loop through each byte of the hashed data 
//                // and format each one as a hexadecimal string.
//                for (int i = 0; i < data.Length; i++)
//                {
//                    sBuilder.Append(data[i].ToString("x2"));
//                }
//
//                return sBuilder.ToString();
//            }
        }
    }
}